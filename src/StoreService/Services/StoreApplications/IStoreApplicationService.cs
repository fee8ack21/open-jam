using StoreService.Models;

namespace StoreService.Services.StoreApplications;

/// <summary>開店申請相關業務邏輯。</summary>
public interface IStoreApplicationService
{
    /// <summary>提交開店申請。同一使用者僅能有一筆 Pending 申請。</summary>
    Task<StoreApplicationDto> SubmitAsync(SubmitStoreApplicationRequest request, CancellationToken ct);

    /// <summary>查詢自己的開店申請紀錄（分頁）。</summary>
    Task<GetStoreApplicationsResponse> GetMineAsync(GetStoreApplicationsRequest request, CancellationToken ct);

    /// <summary>撤回自己的待審核申請（Pending → Withdrawn）。</summary>
    Task WithdrawAsync(Guid id, CancellationToken ct);

    /// <summary>查詢全平台開店申請列表，可依審核狀態篩選（分頁）。</summary>
    Task<GetStoreApplicationsResponse> GetAllAsync(GetStoreApplicationsRequest request, CancellationToken ct);

    /// <summary>核准開店申請（Pending → Approved），建立 Store 與 StoreMember(Owner)。</summary>
    Task<StoreApplicationDto> ApproveAsync(Guid id, CancellationToken ct);

    /// <summary>駁回開店申請（Pending → Rejected）。</summary>
    Task<StoreApplicationDto> RejectAsync(Guid id, RejectStoreApplicationRequest request, CancellationToken ct);
}
