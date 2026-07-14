using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PaymentService.Data;
using PaymentService.Data.Entities;
using PaymentService.Models;
using PaymentService.Options;
using Shared.Auth;
using Stripe;

namespace PaymentService.Services.Connect;

public class ConnectAccountService(
    PaymentDbContext db,
    IOptions<StripeOptions> stripeOptions,
    ICurrentUserAccessor currentUser,
    AuditLogPublisher auditLog,
    ILogger<ConnectAccountService> logger) : IConnectAccountService
{
    /// <inheritdoc/>
    public async Task<AccountLinkResponse> CreateOnboardingLinkAsync(Guid storeId, CancellationToken ct)
    {
        var opts = stripeOptions.Value;
        StripeConfiguration.ApiKey = opts.SecretKey;

        var account = await GetOrCreateAccountAsync(storeId, ct);

        var link = await new AccountLinkService().CreateAsync(new AccountLinkCreateOptions
        {
            Account = account.StripeAccountId,
            RefreshUrl = opts.ConnectRefreshUrl,
            ReturnUrl = opts.ConnectReturnUrl,
            Type = "account_onboarding",
        }, cancellationToken: ct);

        return new AccountLinkResponse { Url = link.Url };
    }

    /// <inheritdoc/>
    public async Task<ConnectAccountStatusResponse> GetStatusAsync(Guid storeId, bool refresh, CancellationToken ct)
    {
        var account = await db.ConnectedAccounts.FirstOrDefaultAsync(a => a.StoreId == storeId, ct);
        if (account is null)
            return new ConnectAccountStatusResponse { HasAccount = false };

        // onboarding 導回當下 webhook 可能尚未送達，refresh 時向 Stripe 取即時狀態回寫本地旗標。
        if (refresh)
        {
            StripeConfiguration.ApiKey = stripeOptions.Value.SecretKey;
            var stripeAccount = await new AccountService().GetAsync(account.StripeAccountId, cancellationToken: ct);
            ApplyStripeAccount(account, stripeAccount);
            await db.SaveChangesAsync(ct);
        }

        return new ConnectAccountStatusResponse
        {
            HasAccount = true,
            DetailsSubmitted = account.DetailsSubmitted,
            ChargesEnabled = account.ChargesEnabled,
            PayoutsEnabled = account.PayoutsEnabled,
        };
    }

    /// <summary>以 Stripe 帳戶即時狀態更新本地旗標（webhook 與 refresh 共用）。</summary>
    public static void ApplyStripeAccount(ConnectedAccount account, Account stripeAccount)
    {
        account.DetailsSubmitted = stripeAccount.DetailsSubmitted;
        account.ChargesEnabled = stripeAccount.ChargesEnabled;
        account.PayoutsEnabled = stripeAccount.PayoutsEnabled;
    }

    private async Task<ConnectedAccount> GetOrCreateAccountAsync(Guid storeId, CancellationToken ct)
    {
        var existing = await db.ConnectedAccounts.FirstOrDefaultAsync(a => a.StoreId == storeId, ct);
        if (existing != null) return existing;

        // destination charge 只需 transfers capability，不請求 card_payments 以降低 onboarding 門檻。
        var stripeAccount = await new AccountService().CreateAsync(new AccountCreateOptions
        {
            Type = "express",
            Capabilities = new AccountCapabilitiesOptions
            {
                Transfers = new AccountCapabilitiesTransfersOptions { Requested = true },
            },
            Metadata = new Dictionary<string, string> { ["store_id"] = storeId.ToString() },
        }, cancellationToken: ct);

        var account = new ConnectedAccount
        {
            Id = Guid.NewGuid(),
            StoreId = storeId,
            StripeAccountId = stripeAccount.Id,
        };
        ApplyStripeAccount(account, stripeAccount);

        db.ConnectedAccounts.Add(account);
        auditLog.Add(currentUser.UserId, "payment.connect.account_created", "ConnectedAccount", account.Id, tenant: storeId);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is Npgsql.PostgresException { SqlState: "23505" })
        {
            // race condition：並行請求已搶先為同商店建立帳戶，重用勝者、放棄本次建立的 Stripe 帳戶（未 onboard 的空帳戶無害）。
            logger.LogWarning("商店 {StoreId} 並發建立連接帳戶，重用既有紀錄並棄用 {StripeAccountId}。",
                storeId, stripeAccount.Id);

            db.ChangeTracker.Clear();
            return await db.ConnectedAccounts.FirstAsync(a => a.StoreId == storeId, ct);
        }

        return account;
    }
}
