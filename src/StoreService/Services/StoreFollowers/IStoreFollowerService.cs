using StoreService.Models;

namespace StoreService.Services.StoreFollowers;

/// <summary>商店追蹤者相關業務邏輯。</summary>
public interface IStoreFollowerService
{
    /// <summary>追蹤商店。已追蹤則 no-op。</summary>
    Task FollowAsync(Guid storeId, FollowStoreRequest request, CancellationToken ct);

    /// <summary>取消追蹤商店。依 (StoreId, Email) 移除，未追蹤則 no-op。</summary>
    Task UnfollowAsync(Guid storeId, FollowStoreRequest request, CancellationToken ct);

    /// <summary>查詢商店追蹤者列表（分頁）。僅 Owner 可操作。</summary>
    Task<GetStoreFollowersResponse> GetFollowersAsync(Guid storeId, GetStoreFollowersRequest request, CancellationToken ct);
}
