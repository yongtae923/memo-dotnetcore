using AblyAPI.Models.Requests;
using AblyAPI.Models.Responses;
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
    /// 전화번호 인증을 받기 위해 전화번호를 입력하고 문자로 인증번호를 보냅니다. 실제로는 인증번호를 문자로 전달해야 하지만, 여기서는 이 API의 응답으로 대신하겠습니다.
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
    public async Task<IActionResult> RequestVerificationCode([FromBody] PhoneNumberRequestModel model)
    {
        var response = await _service.RequestVerificationCodeAsync(model);
        return response.Status switch
        {
            StatusType.Success => Ok(response.Body),
            _ => BadRequest()
        };
    }

    /// <summary>
    /// 전화번호와 인증코드를 입력받아서 해당하는 코드가 있으면 그 코드를 인증 처리합니다.
    /// </summary>
    /// <param name="code">인증번호</param>
    /// <param name="model">전화번호</param>
    /// <remarks>
    /// Sample request:
    /// 
    ///     POST /api/auth/codes/123456
    ///     {
    ///         "phone" : "01012345678"
    ///     }
    /// 
    /// </remarks>
    /// <response code="200">성공</response>
    /// <response code="400">입력한 전화번호가 전화번호 형식이 아니면 실패</response>
    /// <response code="404">입력값에 해당하는 인증코드가 없으면 실패</response>
    /// <response code="408">입력값에 해당하는 인증코드가 모두 만료되었으면 실패</response>
    [HttpPost("codes/{code}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status408RequestTimeout)]
    public async Task<IActionResult> VerifyCode(string code, [FromBody] PhoneNumberRequestModel model)
    {
        var response = await _service.VerifyCodeAsync(code, model);
        return response.Status switch
        {
            StatusType.Success => Ok(),
            StatusType.BadRequest => BadRequest(),
            StatusType.NotFound => NotFound(),
            _ => StatusCode(408)
        };
    }

    /// <summary>
    /// 올바른 회원가입 입력모델을 받아서 회원가입합니다.
    /// </summary>
    /// <param name="model">회원가입 입력모델: 이메일, 비밀번호, 이름, 닉네임, 전화번호</param>
    /// <remarks>
    /// Sample request:
    /// 
    ///     POST /api/auth/accounts
    ///     {
    ///         "email" : "yongtae@a-bly.com",
    ///         "password" : "yongtae@ably!",
    ///         "name" : "Yongtae Kim",
    ///         "nickname" : "Ably-dev",
    ///         "phone" : "01012345678"
    ///     }
    /// 
    /// </remarks>
    /// <response code="200">성공</response>
    /// <response code="400">입력한 전화번호나 이메일이 올바른 형식이 아니면 실패</response>
    /// <response code="403">입력한 전화번호에 해당하는 활성 인증코드가 없으면 실패</response>
    /// <response code="409">입력한 전화번호나 이메일이 겹치는 계정이 이미 있으면 실패</response>
    [HttpPost("accounts")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequestModel model)
    {
        var response = await _service.RegisterAsync(model);
        return response.Status switch
        {
            StatusType.Success => Ok(),
            StatusType.BadRequest => BadRequest(response.Body),
            StatusType.Forbidden => StatusCode(403),
            _ => Conflict(response.Body)
        };
    }
}