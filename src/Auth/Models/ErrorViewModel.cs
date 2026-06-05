namespace Auth.Models;

/// <summary>錯誤頁 ViewModel。</summary>
public class ErrorViewModel
{
    /// <summary>請求識別碼，用於追蹤錯誤來源。</summary>
    /// <example>00-84b765e03a684edc9d84cac74a24f79e-f02d8d52b3d3a0a8-00</example>
    public string? RequestId { get; set; }

    /// <summary>是否顯示請求識別碼（RequestId 非空時為 true）。</summary>
    /// <example>true</example>
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}
