using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PaymentService.Data;
using PaymentService.Data.Entities;
using PaymentService.Options;
using PaymentService.Services.Connect;
using Shared.Events;
using Stripe;

namespace PaymentService.Services.Payments;

public class StripeWebhookHandler(
    PaymentDbContext db,
    IOptions<StripeOptions> stripeOptions,
    ILogger<StripeWebhookHandler> logger)
{
    /// <summary>驗證簽章並落地原始事件後立即返回，業務處理交由 <c>StripeWebhookProcessorService</c> 非同步執行，
    /// 避免服務在處理過程中掛掉導致 webhook 遺失（事件已落地，重啟後仍會被排程處理）。</summary>
    public Task<string> ReceiveAsync(string body, string signature, CancellationToken ct) =>
        ReceiveCoreAsync(body, signature, stripeOptions.Value.WebhookSecret, ct);

    /// <summary>接收 Connect webhook（account.updated 等連接帳戶事件），簽章密鑰與平台端點不同。</summary>
    public Task<string> ReceiveConnectAsync(string body, string signature, CancellationToken ct) =>
        ReceiveCoreAsync(body, signature, stripeOptions.Value.ConnectWebhookSecret, ct);

    private async Task<string> ReceiveCoreAsync(string body, string signature, string secret, CancellationToken ct)
    {
        // throwOnApiVersionMismatch: false —— Stripe 帳戶事件的 API 版本（如 2026-06-24.dahlia）
        // 常新於 Stripe.net 函式庫預期版本，預設會拋例外致 webhook 回錯、事件無法落地。簽章仍照常驗
        // （secret 有傳），且本服務只取 session id / metadata 等穩定欄位，版本 skew 安全。與 ParseEvent 一致。
        var evt = EventUtility.ConstructEvent(body, signature, secret, throwOnApiVersionMismatch: false);
        var eventId = evt.Id;
        var eventType = evt.Type;

        logger.LogInformation("Received Stripe webhook: {EventId} {EventType}", eventId, eventType);

        var existing = await db.ProviderEvents
            .FirstOrDefaultAsync(e => e.Provider == "stripe" && e.EventId == eventId, ct);

        if (existing != null)
        {
            logger.LogInformation("Duplicate webhook skipped: {EventId}", eventId);
            return eventType;
        }

        db.ProviderEvents.Add(new ProviderEvent
        {
            Id = Guid.NewGuid(),
            Provider = "stripe",
            EventId = eventId,
            EventType = eventType,
            RawPayload = body,
        });

        await db.SaveChangesAsync(ct);

        return eventType;
    }

    /// <summary>由 <c>StripeWebhookProcessorService</c> 排程呼叫，處理尚未完成的事件。呼叫端負責 <c>SaveChangesAsync</c>。</summary>
    public async Task ProcessPendingAsync(ProviderEvent providerEvent, CancellationToken ct)
    {
        var opts = stripeOptions.Value;
        StripeConfiguration.ApiKey = opts.SecretKey;

        var evt = EventUtility.ParseEvent(providerEvent.RawPayload, throwOnApiVersionMismatch: false);

        switch (evt.Type)
        {
            case "checkout.session.completed":
            case "checkout.session.async_payment_succeeded":
                await HandleCheckoutCompletedAsync(evt, ct);
                break;
            case "checkout.session.async_payment_failed":
                await HandleCheckoutFailedAsync(evt, ct);
                break;
            case "checkout.session.expired":
                await HandleCheckoutExpiredAsync(evt, ct);
                break;
            case "payment_intent.payment_failed":
                await HandlePaymentFailedAsync(evt, ct);
                break;
            case "account.updated":
                await HandleAccountUpdatedAsync(evt, ct);
                break;
            default:
                logger.LogInformation("Unhandled webhook type: {EventType}", evt.Type);
                break;
        }

        providerEvent.ProcessedAt = DateTimeOffset.UtcNow;
    }

    private async Task HandleCheckoutCompletedAsync(Event evt, CancellationToken ct)
    {
        var session = evt.Data.Object as Stripe.Checkout.Session;
        if (session == null) return;

        var paymentId = GetPaymentId(session);
        if (paymentId == null) return;

        var payment = await db.Payments.FirstOrDefaultAsync(p => p.Id == paymentId.Value, ct);
        if (payment == null) return;

        if (payment.Status == PaymentStatus.Succeeded) return;

        payment.Status = PaymentStatus.Succeeded;
        payment.ProviderPaymentId = session.PaymentIntentId ?? payment.ProviderPaymentId;
        payment.PaidAt = DateTimeOffset.UtcNow;

        db.PaymentTransactions.Add(new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            PaymentId = payment.Id,
            TransactionType = TransactionType.Success,
            ProviderTransactionId = session.PaymentIntentId ?? session.Id,
            RawPayload = evt.Data.RawObject?.ToJson(),
        });

        var outbox = new OutboxMessage
        {
            EventType = "payment.succeeded",
            Payload = JsonSerializer.Serialize(new PaymentSucceededEvent(
                OutboxMessageId: Guid.NewGuid(),
                PaymentId: payment.Id,
                OrderId: payment.OrderId,
                UserId: payment.UserId,
                Email: payment.Email,
                Amount: payment.Amount,
                Currency: payment.Currency,
                ProviderPaymentId: payment.ProviderPaymentId ?? "",
                PaidAt: payment.PaidAt ?? DateTimeOffset.UtcNow
            ), OutboxJson.Options),
        };
        db.OutboxMessages.Add(outbox);

        logger.LogInformation("Payment succeeded: {PaymentId} {CheckoutSessionId}", payment.Id, session.Id);
    }

    private async Task HandleCheckoutFailedAsync(Event evt, CancellationToken ct)
    {
        var session = evt.Data.Object as Stripe.Checkout.Session;
        if (session == null) return;

        var paymentId = GetPaymentId(session);
        if (paymentId == null) return;

        var payment = await db.Payments.FirstOrDefaultAsync(p => p.Id == paymentId.Value, ct);
        if (payment == null) return;

        // 只有仍是 Pending 才標記失敗；遲到 / 亂序的失敗事件不得覆蓋已成功的付款。
        if (payment.Status != PaymentStatus.Pending) return;

        payment.Status = PaymentStatus.Failed;
        payment.FailedAt = DateTimeOffset.UtcNow;

        db.PaymentTransactions.Add(new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            PaymentId = payment.Id,
            TransactionType = TransactionType.Fail,
            ProviderTransactionId = session.Id,
            RawPayload = evt.Data.RawObject?.ToJson(),
        });

        logger.LogInformation("Payment failed (async): {PaymentId} {CheckoutSessionId}", payment.Id, session.Id);
    }

    private async Task HandleCheckoutExpiredAsync(Event evt, CancellationToken ct)
    {
        var session = evt.Data.Object as Stripe.Checkout.Session;
        if (session == null) return;

        var paymentId = GetPaymentId(session);
        if (paymentId == null) return;

        var payment = await db.Payments.FirstOrDefaultAsync(p => p.Id == paymentId.Value, ct);
        if (payment == null) return;

        // 只有仍是 Pending 才標記過期；若已成功 / 失敗則維持原狀態。
        if (payment.Status != PaymentStatus.Pending) return;

        payment.Status = PaymentStatus.Expired;
        payment.ExpiredAt = DateTimeOffset.UtcNow;

        db.PaymentTransactions.Add(new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            PaymentId = payment.Id,
            TransactionType = TransactionType.Expired,
            ProviderTransactionId = session.Id,
            RawPayload = evt.Data.RawObject?.ToJson(),
        });

        logger.LogInformation("Checkout session expired: {PaymentId} {CheckoutSessionId}", payment.Id, session.Id);
    }

    private async Task HandlePaymentFailedAsync(Event evt, CancellationToken ct)
    {
        var intent = evt.Data.Object as PaymentIntent;
        if (intent == null) return;

        var payment = await db.Payments
            .FirstOrDefaultAsync(p => p.ProviderPaymentId == intent.Id || p.ProviderCheckoutId == intent.Id, ct);
        if (payment == null) return;

        // 只有仍是 Pending 才標記失敗（Checkout 內刷卡失敗後重試成功是常見流程，失敗事件可能晚到）。
        if (payment.Status != PaymentStatus.Pending) return;

        payment.Status = PaymentStatus.Failed;
        payment.FailedAt = DateTimeOffset.UtcNow;

        db.PaymentTransactions.Add(new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            PaymentId = payment.Id,
            TransactionType = TransactionType.Fail,
            ProviderTransactionId = intent.Id,
            RawPayload = evt.Data.RawObject?.ToJson(),
        });

        logger.LogInformation("Payment failed: {PaymentId} {PaymentIntentId}", payment.Id, intent.Id);
    }

    private async Task HandleAccountUpdatedAsync(Event evt, CancellationToken ct)
    {
        var stripeAccount = evt.Data.Object as Account;
        if (stripeAccount == null) return;

        var account = await db.ConnectedAccounts
            .FirstOrDefaultAsync(a => a.StripeAccountId == stripeAccount.Id, ct);
        if (account == null)
        {
            logger.LogWarning("收到未知連接帳戶的 account.updated：{StripeAccountId}", stripeAccount.Id);
            return;
        }

        ConnectAccountService.ApplyStripeAccount(account, stripeAccount);

        logger.LogInformation(
            "Connected account updated: {StripeAccountId} charges={ChargesEnabled} payouts={PayoutsEnabled}",
            stripeAccount.Id, account.ChargesEnabled, account.PayoutsEnabled);
    }

    private static Guid? GetPaymentId(Stripe.Checkout.Session session)
    {
        if (session.Metadata.TryGetValue("payment_id", out var pid) && Guid.TryParse(pid, out var id))
            return id;
        return null;
    }
}
