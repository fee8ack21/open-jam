namespace PaymentService.Models;

/// <summary>Stripe Connect onboarding Account Link 回應。</summary>
public class AccountLinkResponse
{
    /// <summary>Stripe 託管 onboarding 頁 URL（短效，前端直接導向）。</summary>
    /// <example>https://connect.stripe.com/setup/e/acct_xxx/yyy</example>
    public string Url { get; set; } = "";
}

/// <summary>商店 Stripe Connect 帳戶狀態。</summary>
public class ConnectAccountStatusResponse
{
    /// <summary>商店是否已建立 Stripe 連接帳戶。</summary>
    /// <example>true</example>
    public bool HasAccount { get; set; }

    /// <summary>創作者是否已提交 onboarding 資料。</summary>
    /// <example>true</example>
    public bool DetailsSubmitted { get; set; }

    /// <summary>帳戶是否可承接款項（付費商品上架與分帳的閘門依據）。</summary>
    /// <example>true</example>
    public bool ChargesEnabled { get; set; }

    /// <summary>帳戶是否可提領出金。</summary>
    /// <example>true</example>
    public bool PayoutsEnabled { get; set; }
}
