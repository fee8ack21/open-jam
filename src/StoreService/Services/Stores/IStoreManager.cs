using StoreService.Models;

namespace StoreService.Services.Stores;

/// <summary>商店相關業務邏輯（查詢、更新、狀態管理、Avatar/Banner 上傳）。</summary>
public interface IStoreManager
{
    /// <summary>查詢商店基本資訊（依 ID 或 StoreSlug）。</summary>
    Task<StoreDto> GetAsync(string idOrSlug, CancellationToken ct);

    /// <summary>查詢登入使用者所屬的商店列表。</summary>
    Task<List<MyStoreDto>> GetMineAsync(CancellationToken ct);

    /// <summary>更新商店基本資料（StoreName / Description）。僅 Owner 可操作。</summary>
    Task<StoreDto> UpdateAsync(Guid id, UpdateStoreRequest request, CancellationToken ct);

    /// <summary>平台停權商店（Active → Suspended）。僅 Admin 可操作。</summary>
    Task SuspendAsync(Guid id, CancellationToken ct);

    /// <summary>解除商店停權（Suspended → Active）。僅 Admin 可操作。</summary>
    Task UnsuspendAsync(Guid id, CancellationToken ct);

    /// <summary>關閉商店（Active/Suspended → Closed，終態不可逆）。Owner 或 Admin 可操作。</summary>
    Task CloseAsync(Guid id, CancellationToken ct);

    /// <summary>申請商店資產（Avatar/Banner）上傳簽章 URL。僅 Owner 可操作。</summary>
    Task<AssetUploadUrlResponse> RequestAssetUploadUrlAsync(
        Guid id, RequestAssetUploadUrlRequest request, bool isAvatar, CancellationToken ct);
}
