using AblyAPI.Filters;
using AblyAPI.Models.Responses;
using AblyAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace AblyAPI.Controllers;

[Route("api/user")]
public class UserController : Controller
{
    private readonly IUserService _service;
    public string AccountId { get; set; }

    public UserController(IUserService service)
    {
        _service = service;
    }
    
    /// <summary>
    /// 사용자 정보를 조회합니다. 실행을 위해서 우측에 엑세스토큰을 입력해주세요.
    /// </summary>
    /// <param name="accountId"></param>
    /// <remarks>
    /// Sample request:
    /// 
    ///     GET /api/user/accounts/{accountId}
    /// 
    /// </remarks>
    /// <response code="200">성공, body에 사용자 정보가 있습니다</response>
    /// <response code="401">접근토큰이 없거나 계정을 찾을 수 없으면 실패</response>
    /// <response code="403">접근토큰으로 찾은 계정과 정보를 얻으려는 계정이 다르면 실패</response>
    [HttpGet("accounts/{accountId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [AuthorizationFilter]
    public async Task<IActionResult> GetUserInformation(string accountId)
    {
        if (accountId != AccountId) return StatusCode(403);
        
        var response = await _service.GetUserInformationAsync(accountId);
        return response.Status switch
        {
            StatusType.Success => Ok(response.Body),
            _ => Unauthorized()
        };
    }

    /// <summary>
    /// 현재 계정의 비밀번호를 변경합니다. 실행을 위해서 다시 전화번호 인증을 하고 우측에 엑세스토큰을 입력해주세요.
    /// </summary>
    /// <param name="accountId"></param>
    /// <param name="newPassword"></param>
    /// <response code="200">성공, body에 사용자 정보가 있습니다</response>
    /// <response code="401">엑세스토큰이 없거나 계정을 찾을 수 없으면 실패</response>
    /// <response code="403">전화번호 인증을 하지 않아 활성화된 엑세스 토큰이 없거나 만료되었으면 실패</response>
    [HttpPut("accounts/{accountId}/credentials")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [AuthorizationFilter]
    public async Task<IActionResult> ChangePassword(string accountId, string newPassword)
    {
        if (accountId != AccountId) return StatusCode(403);

        var response = await _service.ChangePasswordAsync(AccountId, newPassword);
        return response.Status switch
        {
            StatusType.Success => Ok(),
            StatusType.Unauthorized => Unauthorized(),
            _ => StatusCode(403)
        };
    }
}