using Shared.Audit;

namespace PaymentService.Data.Entities;

/// <summary>商店的 Stripe Connect 連接帳戶（Express），承接分帳款項。</summary>
public class ConnectedAccount : ICreatedAt, IUpdatedAt
{
    public Guid Id { get; set; }

    /// <summary>所屬商店 ID（StoreService），一店一帳戶。</summary>
    public Guid StoreId { get; set; }

    /// <summary>Stripe 帳戶 ID（acct_xxx）。</summary>
    public string StripeAccountId { get; set; } = "";

    /// <summary>創作者是否已提交 onboarding 資料（Stripe `details_submitted`）。</summary>
    public bool DetailsSubmitted { get; set; }

    /// <summary>帳戶是否可承接款項（Stripe `charges_enabled`），上架付費商品與分帳的閘門依據。</summary>
    public bool ChargesEnabled { get; set; }

    /// <summary>帳戶是否可提領出金（Stripe `payouts_enabled`）。</summary>
    public bool PayoutsEnabled { get; set; }

    /// <inheritdoc/>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <inheritdoc/>
    public DateTimeOffset? UpdatedAt { get; private set; }
}
