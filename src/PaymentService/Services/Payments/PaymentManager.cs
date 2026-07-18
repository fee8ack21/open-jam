using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PaymentService.Data;
using PaymentService.Data.Entities;
using PaymentService.Models;
using PaymentService.Options;
using Shared.Exceptions;
using Stripe;
using Stripe.Checkout;

namespace PaymentService.Services.Payments;

public class PaymentManager(
    PaymentDbContext db,
    IMapper mapper,
    IOptions<StripeOptions> stripeOptions,
    StoreServiceClient storeService,
    AuditLogPublisher auditLog) : IPaymentManager
{
    // 付款成功 / 取消導回 URL 模板中的店面子網域佔位符（如 https://{storeSlug}.openjam.co/...）。
    private const string StoreSlugToken = "{storeSlug}";


    public async Task<CheckoutSessionResponse> CreateCheckoutSessionAsync(
        CreateCheckoutSessionRequest request, CancellationToken ct)
    {
        // 分帳目的地：商店須已完成 Stripe onboarding 且可承接款項（上架閘門的最後防線，
        // 涵蓋上架後帳戶被 Stripe 停用等狀態回退情境）。此閘門必須先於 Pending 付款重用檢查：
        // 帳戶停用後，同訂單既有 session（最長存活 24 小時）不得再被遞出，並順手作廢，
        // 避免買家對已停用的目的地帳戶付款導致款項懸置。
        var connectedAccount = await db.ConnectedAccounts
            .FirstOrDefaultAsync(a => a.StoreId == request.StoreId, ct);

        if (connectedAccount is not { ChargesEnabled: true })
        {
            await ExpireCheckoutByOrderAsync(request.OrderId, ct);
            throw new ValidationException("此商店尚未完成收款設定，暫時無法接受付款。");
        }

        // 同一訂單已有未過期的 Pending 付款時直接重用，避免使用者重複建立 Checkout Session（如重複點擊購買鈕）。
        var existing = await db.Payments
            .Where(p => p.OrderId == request.OrderId
                && p.Status == PaymentStatus.Pending
                && p.ExpiresAt > DateTimeOffset.UtcNow
                && p.CheckoutUrl != null)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (existing != null)
        {
            return new CheckoutSessionResponse
            {
                PaymentId = existing.Id,
                SessionId = existing.ProviderCheckoutId ?? "",
                Url = existing.CheckoutUrl!,
            };
        }

        var opts = stripeOptions.Value;
        StripeConfiguration.ApiKey = opts.SecretKey;

        // 導回 URL 以買家原本所在的店面子網域組出（結帳結果頁 CheckoutResultView 位於 creator-web，
        // 只服務 <slug>.openjam.co；導回 apex 會落到沒有此路由的 portal-web）。模板含 {storeSlug} 時
        // 才查 StoreService 取代（本地開發單一 creator-web 無子網域、模板不帶佔位符則免查）。
        var (successUrl, cancelUrl) = await ResolveReturnUrlsAsync(opts, request.StoreId, ct);

        // 平台抽成（application fee）＝百分比部分（四捨五入至最低貨幣單位）＋固定費。固定費用以吸收
        // Stripe 每筆約 $0.30 的固定手續費，否則低單價訂單光百分比抽成無法打平（見 PlatformFeeFixed）。
        // 抽成不得超過訂單金額（Stripe 約束 application_fee_amount ≤ amount）——上架最低定價已擋下過小
        // 金額，此處再以 Min 防禦性夾住，避免異常金額導致 Stripe 拒絕。
        var rawFee = (long)Math.Round(
            request.Amount * opts.PlatformFeePercent / 100m, MidpointRounding.AwayFromZero)
            + opts.PlatformFeeFixed;
        var applicationFee = Math.Min(rawFee, request.Amount);

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            OrderId = request.OrderId,
            StoreId = request.StoreId,
            UserId = request.UserId,
            Email = request.Email,
            Amount = request.Amount,
            Currency = request.Currency.ToLowerInvariant(),
            Status = PaymentStatus.Pending,
            DestinationAccountId = connectedAccount.StripeAccountId,
            ApplicationFeeAmount = applicationFee,
        };

        var options = new SessionCreateOptions
        {
            Mode = "payment",
            SuccessUrl = successUrl,
            CancelUrl = cancelUrl,
            CustomerEmail = request.Email,
            LineItems =
            [
                new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmount = request.Amount,
                        Currency = request.Currency.ToLowerInvariant(),
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = request.ProductName,
                        },
                    },
                    Quantity = 1,
                },
            ],
            Metadata = new Dictionary<string, string>
            {
                ["payment_id"] = payment.Id.ToString(),
                ["order_id"] = request.OrderId.ToString(),
            },
            // destination charge 分帳：款項自動轉入商店的連接帳戶，平台留下 application fee。
            PaymentIntentData = new SessionPaymentIntentDataOptions
            {
                TransferData = new SessionPaymentIntentDataTransferDataOptions
                {
                    Destination = connectedAccount.StripeAccountId,
                },
                ApplicationFeeAmount = applicationFee > 0 ? applicationFee : null,
            },
        };

        var service = new SessionService();
        Session session;
        try
        {
            session = await service.CreateAsync(options, cancellationToken: ct);
        }
        catch (StripeException ex)
        {
            // Stripe 拒絕（如金額低於最低收款門檻約 $0.50 USD、幣別 / 帳戶問題）：轉為 422 並帶
            // Stripe 原始訊息，而非讓非 AppException 冒泡成毫無資訊的 500。OrderService 的
            // PaymentServiceClient 會取 Problem Details detail 原樣轉給前端顯示。
            throw new ValidationException($"金流服務無法建立付款：{ex.StripeError?.Message ?? ex.Message}");
        }

        payment.ProviderCheckoutId = session.Id;
        payment.ProviderPaymentId = session.PaymentIntentId;
        payment.CheckoutUrl = session.Url;
        payment.ExpiresAt = new DateTimeOffset(session.ExpiresAt, TimeSpan.Zero);

        db.Payments.Add(payment);
        db.PaymentTransactions.Add(new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            PaymentId = payment.Id,
            TransactionType = TransactionType.Created,
            ProviderTransactionId = session.Id,
        });

        auditLog.Add(
            who: request.UserId,
            action: "payment.checkout.created",
            target: "Payment",
            targetId: payment.Id,
            tenant: null);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            // race condition：另一個並行請求已搶先建立同訂單的 Pending 付款，重用對方的 Checkout Session。
            var winner = await db.Payments
                .Where(p => p.OrderId == request.OrderId && p.Status == PaymentStatus.Pending)
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync(ct);

            if (winner == null) throw;

            return new CheckoutSessionResponse
            {
                PaymentId = winner.Id,
                SessionId = winner.ProviderCheckoutId ?? "",
                Url = winner.CheckoutUrl ?? "",
            };
        }

        return new CheckoutSessionResponse
        {
            PaymentId = payment.Id,
            SessionId = session.Id,
            Url = session.Url,
        };
    }

    /// <summary>
    /// 將付款成功 / 取消導回 URL 模板中的 <c>{storeSlug}</c> 換成該商店子網域代稱。
    /// 兩個模板皆不含佔位符時（如本地開發）直接沿用、不呼叫 StoreService。
    /// <c>{CHECKOUT_SESSION_ID}</c> 保持原樣由 Stripe 代入。
    /// </summary>
    private async Task<(string SuccessUrl, string CancelUrl)> ResolveReturnUrlsAsync(
        StripeOptions opts, Guid storeId, CancellationToken ct)
    {
        if (!opts.SuccessUrl.Contains(StoreSlugToken) && !opts.CancelUrl.Contains(StoreSlugToken))
            return (opts.SuccessUrl, opts.CancelUrl);

        var slug = await storeService.GetStoreSlugAsync(storeId, ct);
        return (
            opts.SuccessUrl.Replace(StoreSlugToken, slug),
            opts.CancelUrl.Replace(StoreSlugToken, slug));
    }

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is Npgsql.PostgresException pgEx && pgEx.SqlState == "23505";

    public async Task ExpireCheckoutByOrderAsync(Guid orderId, CancellationToken ct)
    {
        var pendings = await db.Payments
            .Where(p => p.OrderId == orderId && p.Status == PaymentStatus.Pending)
            .ToListAsync(ct);

        // 無 Pending 付款（免費訂單、Session 從未建成、或先前重試已作廢完成）：冪等視為成功。
        if (pendings.Count == 0) return;

        StripeConfiguration.ApiKey = stripeOptions.Value.SecretKey;
        var sessions = new SessionService();

        foreach (var payment in pendings)
        {
            if (payment.ProviderCheckoutId is null)
            {
                MarkExpired(payment, providerTransactionId: null);
                continue;
            }

            // 以 Stripe 現況仲裁：只有它知道這筆到底收了沒。open 才作廢；complete 拒絕取消；
            // expired（自然過期或前次重試已作廢但本地未落地）直接補標記，重放安全。
            var session = await sessions.GetAsync(payment.ProviderCheckoutId, cancellationToken: ct);

            if (session.Status == "open")
            {
                try
                {
                    await sessions.ExpireAsync(payment.ProviderCheckoutId, cancellationToken: ct);
                    session = null;
                }
                catch (StripeException)
                {
                    // Get 與 Expire 之間的競態（買家恰於此刻完成付款等）：重查一次以現況為準。
                    session = await sessions.GetAsync(payment.ProviderCheckoutId, cancellationToken: ct);
                    if (session.Status != "expired" && session.Status != "complete") throw;
                }
            }

            if (session?.Status == "complete")
                throw new ConflictException("訂單已完成付款，無法取消。");

            MarkExpired(payment, payment.ProviderCheckoutId);
            auditLog.Add(
                who: payment.UserId,
                action: "payment.checkout.expired",
                target: "Payment",
                targetId: payment.Id,
                tenant: payment.StoreId);
        }

        await db.SaveChangesAsync(ct);
    }

    /// <summary>將付款標記為過期並記一筆交易歷程（與 webhook 的 checkout.session.expired 處理一致；
    /// 該 webhook 事件稍後送達時因狀態已非 Pending 而自然略過，不會重複記錄）。</summary>
    private void MarkExpired(Payment payment, string? providerTransactionId)
    {
        payment.Status = PaymentStatus.Expired;
        payment.ExpiredAt = DateTimeOffset.UtcNow;

        db.PaymentTransactions.Add(new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            PaymentId = payment.Id,
            TransactionType = TransactionType.Expired,
            ProviderTransactionId = providerTransactionId,
        });
    }

    public async Task<PaymentResponse> GetAsync(Guid id, CancellationToken ct)
    {
        var payment = await db.Payments.FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new NotFoundException("Payment not found.");

        return mapper.Map<PaymentResponse>(payment);
    }
}
