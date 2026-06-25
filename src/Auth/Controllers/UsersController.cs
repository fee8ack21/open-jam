using Asp.Versioning;
using Auth.Models;
using Auth.Services.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Auth.Controllers;

/// <summary>
/// 平台使用者查詢 REST API（管理員後台使用）。
/// 僅具 "Admin" 角色的 Hydra access token 可存取。
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/users")]
[Authorize(Roles = "Admin")]
public class UsersController(IUserService userService) : ControllerBase
{
    /// <summary>查詢平台使用者（分頁，支援關鍵字 / 角色 / 狀態篩選）。</summary>
    /// <param name="request">篩選與分頁參數。</param>
    /// <param name="ct">Cancellation token。</param>
    /// <returns>符合條件的使用者分頁結果。</returns>
    [HttpGet]
    [ProducesResponseType<ListUsersResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ListUsersResponse>> ListAsync([FromQuery] ListUsersRequest request, CancellationToken ct)
        => Ok(await userService.ListAsync(request, ct));
}
