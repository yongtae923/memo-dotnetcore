using AblyAPI.Models.DTO;
using AblyAPI.Models.Requests;
using AblyAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace AblyAPI.Controllers;

[Route("api/auth")]
public class AuthController : Controller
{
    private readonly IAuthService _service;

    public AuthController(IAuthService service)
    {
        _service = service;
    }
    
    /// <summary>
    /// 전화번호 인증을 받기 위해 전화번호를 입력하고 문자로 인증번호를 보내는 API. 실제로는 인증번호를 문자로 전달해야 하지만, 여기서는 이 API의 응답으로 대신하겠습니다.
    /// </summary>
    /// <param name="model">전화번호</param>
    /// <remarks>
    /// Sample request:
    /// 
    ///     POST /api/auth/codes
    ///     {
    ///         "phone" : "01012345678"
    ///     }
    ///
    /// </remarks>
    /// <response code="200">성공 시 인증번호 응답</response>
    /// <response code="400">입력값이 전화번호 형식이 아니면 실패</response>
    [HttpPost("codes")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RequestVerificationCode([FromBody] VerificationCodeRequestModel model)
    {
        var response = await _service.RequestVerificationCodeAsync(model);
        return response.Status switch
        {
            StatusType.Success => Ok(response.Body),
            _ => BadRequest()
        };
    }
}