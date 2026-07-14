using Asp.Versioning;
using ContentService.Data.Entities;
using ContentService.Models;
using ContentService.Services.Legal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContentService.Controllers;

/// <summary>
/// 法律文件（服務條款 / 隱私權政策）版本管理 REST API。
/// 管理端點僅具 "Admin" 角色的 Hydra access token 可存取；
/// 「目前啟用版本」查詢為匿名公開（portal-web 條款頁與 Auth 註冊 / 登入同意流程呈現用）。
/// 文件不可刪除（無 DELETE 端點），停用後仍保留供歷史比對。
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/legal-documents")]
[Authorize(Roles = "Admin")]
public class LegalDocumentsController(ILegalDocumentService legalDocumentService) : ControllerBase
{
    /// <summary>查詢法律文件（分頁，支援類型 / 狀態篩選）。</summary>
    /// <param name="request">篩選與分頁參數。</param>
    /// <param name="ct">Cancellation token。</param>
    /// <returns>符合條件的文件分頁結果（不含完整內容）。</returns>
    [HttpGet]
    [ProducesResponseType<ListLegalDocumentsResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ListLegalDocumentsResponse>> List([FromQuery] ListLegalDocumentsRequest request, CancellationToken ct)
        => Ok(await legalDocumentService.ListAsync(request, ct));

    /// <summary>取得目前啟用中的法律文件內容（匿名公開）。</summary>
    /// <param name="type">文件類型；不帶時回傳所有類型的啟用版本。</param>
    /// <param name="ct">Cancellation token。</param>
    /// <returns>啟用中的文件清單（含完整內容）。</returns>
    [HttpGet("active")]
    [AllowAnonymous]
    [ProducesResponseType<List<LegalDocumentDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<LegalDocumentDto>>> GetActive([FromQuery] LegalDocumentType? type, CancellationToken ct)
        => Ok(await legalDocumentService.GetActiveAsync(type, ct));

    /// <summary>取得單筆法律文件完整內容。</summary>
    /// <param name="id">文件 ID。</param>
    /// <param name="ct">Cancellation token。</param>
    /// <returns>文件完整內容。</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<LegalDocumentDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LegalDocumentDto>> Get(Guid id, CancellationToken ct)
        => Ok(await legalDocumentService.GetAsync(id, ct));

    /// <summary>建立法律文件草稿；版本序號由伺服器依同類型現有最大版本 +1 產生。</summary>
    /// <param name="request">文件類型、標題與內容。</param>
    /// <param name="ct">Cancellation token。</param>
    /// <returns>建立完成的草稿。</returns>
    [HttpPost]
    [ProducesResponseType<LegalDocumentDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<LegalDocumentDto>> Create([FromBody] CreateLegalDocumentRequest request, CancellationToken ct)
    {
        var dto = await legalDocumentService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = dto.Id, version = "1" }, dto);
    }

    /// <summary>更新法律文件草稿的標題與內容；僅 Draft 狀態可更新。</summary>
    /// <param name="id">文件 ID。</param>
    /// <param name="request">更新後的標題與內容。</param>
    /// <param name="ct">Cancellation token。</param>
    /// <returns>更新後的文件。</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType<LegalDocumentDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<LegalDocumentDto>> Update(Guid id, [FromBody] UpdateLegalDocumentRequest request, CancellationToken ct)
        => Ok(await legalDocumentService.UpdateAsync(id, request, ct));

    /// <summary>啟用文件（Draft / Inactive → Active）；同類型既有啟用版本自動轉為 Inactive。</summary>
    /// <param name="id">文件 ID。</param>
    /// <param name="ct">Cancellation token。</param>
    /// <returns>啟用後的文件。</returns>
    [HttpPost("{id:guid}/activate")]
    [ProducesResponseType<LegalDocumentDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LegalDocumentDto>> Activate(Guid id, CancellationToken ct)
        => Ok(await legalDocumentService.ActivateAsync(id, ct));

    /// <summary>停用啟用中的文件（Active → Inactive）。</summary>
    /// <param name="id">文件 ID。</param>
    /// <param name="ct">Cancellation token。</param>
    /// <returns>停用後的文件。</returns>
    [HttpPost("{id:guid}/deactivate")]
    [ProducesResponseType<LegalDocumentDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<LegalDocumentDto>> Deactivate(Guid id, CancellationToken ct)
        => Ok(await legalDocumentService.DeactivateAsync(id, ct));
}
