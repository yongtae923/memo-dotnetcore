using AblyAPI.Models.Responses;
using AblyAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace AblyAPI.Controllers;

[Route("api/user")]
public class UserController : Controller
{
    private readonly IUserService _service;

    public UserController(IUserService service)
    {
        _service = service;
    }
    
    [HttpGet("accounts/{accountId}")]
    public async Task<IActionResult> GetUserInformation(string accountId)
    {
        var token = HttpContext.Request.Headers.Authorization.FirstOrDefault();
        
        var response = await _service.GetUserInformation();
        return response.Status switch
        {
            StatusType.Success => Ok()
        };
    }
}