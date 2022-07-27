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
    /// 올바른 회원가입 입력모델을 받아서 회원가입합니다.
    /// </summary>
    /// <param name="accountId"></param>
    /// <remarks>
    /// Sample request:
    /// 
    ///     GET /api/user/accounts/{accountId}
    /// 
    /// </remarks>
    /// <response code="200">성공, body에 사용자 정보가 있습니다</response>
    /// <response code="401">접근토큰이 없거나 찾을 수 없으면 실패</response>
    /// <response code="403">접근토큰으로 찾은 계정이 입력값과 다르면 실패</response>
    [HttpGet("accounts/{accountId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [AuthorizationFilter]
    public async Task<IActionResult> GetUserInformation(string accountId)
    {
        var response = await _service.GetUserInformationAsync(accountId, AccountId);
        return response.Status switch
        {
            StatusType.Success => Ok(response.Body),
            StatusType.Unauthorized => Unauthorized(),
            _ => StatusCode(403)
        };
    }
}