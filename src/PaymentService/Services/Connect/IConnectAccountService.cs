using PaymentService.Models;

namespace PaymentService.Services.Connect;

/// <summary>Stripe Connect 連接帳戶業務邏輯。</summary>
public interface IConnectAccountService
{
    /// <summary>為商店建立（或重用）Stripe Express 帳戶並簽發 onboarding Account Link。</summary>
    Task<AccountLinkResponse> CreateOnboardingLinkAsync(Guid storeId, CancellationToken ct);

    /// <summary>查詢商店連接帳戶狀態；<paramref name="refresh"/> 時向 Stripe 取得即時狀態並回寫本地旗標。</summary>
    Task<ConnectAccountStatusResponse> GetStatusAsync(Guid storeId, bool refresh, CancellationToken ct);

    /// <summary>簽發商店 Stripe Express Dashboard 登入連結（短效），供創作者查看餘額、撥款排程與交易明細。</summary>
    Task<AccountLinkResponse> CreateLoginLinkAsync(Guid storeId, CancellationToken ct);
}
