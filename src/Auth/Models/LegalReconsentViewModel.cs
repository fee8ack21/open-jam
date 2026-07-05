using System.ComponentModel.DataAnnotations;
using Auth.Common.Validators;

namespace Auth.Models;

/// <summary>登入時新版條款重新同意（re-consent）表單 ViewModel。</summary>
public class LegalReconsentViewModel
{
    /// <summary>是否同意目前啟用中的條款版本。</summary>
    /// <example>true</example>
    [MustBeTrue(ErrorMessage = "請先同意服務條款")]
    public bool Agree { get; set; }

    /// <summary>
    /// 已通過密碼（或 Hydra session）驗證的登入票證，
    /// 由 Data Protection 保護的 <see cref="ReconsentTicket"/>，防止未驗證者偽造同意。
    /// </summary>
    [Required]
    public string Ticket { get; set; } = "";

    /// <summary>需重新同意的啟用中文件（僅供畫面渲染，POST 時由伺服器重查，不從表單回傳）。</summary>
    public List<LegalDocumentDto> Documents { get; set; } = [];
}

/// <summary>
/// re-consent 流程的登入票證內容；於登入驗證成功當下以
/// time-limited Data Protection 序列化保護，同意送出時解回並完成 Hydra AcceptLogin。
/// </summary>
public record ReconsentTicket(string Subject, string Challenge, bool Remember);
