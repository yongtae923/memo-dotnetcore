using System.Net.Http.Headers;
using AblyAPI.Controllers;
using AblyAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace AblyAPI.Filters;

public class AuthorizationFilter : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // request header에서 authorization token을 파싱할 수 없으면 Unauthorized를 반환합니다.
        if (!AuthenticationHeaderValue.TryParse(context.HttpContext.Request.Headers.Authorization.ToString(),
                out var value)) ReturnUnauthorized(context, next);

        // request header에서 authorization token이 없으면 Unauthorized를 반환합니다.
        if (value?.Parameter is null) ReturnUnauthorized(context, next);

        // AuthorizationFilter에서 DatabaseContext에 접근할 수 없으면 Unauthorized를 반환합니다.
        var database = context.HttpContext.RequestServices.GetService<DatabaseContext>();
        if (database is null) ReturnUnauthorized(context, next);

        // request header에서 파싱한 token이 Database에서 찾을 수 없으면 Unauthorized를 반환합니다.
        var token = await database!.AccessTokens.SingleOrDefaultAsync(token => token.Token == value!.Parameter);
        if (token is null) ReturnUnauthorized(context, next);
        
        // 토큰의 계정 아이디를 UserController에 저장합니다.
        ((UserController) context.Controller).AccountId = token!.AccountId;
        await next();
    }

    private static async void ReturnUnauthorized(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        context.Result = new UnauthorizedObjectResult(context.HttpContext.Request.Path);
        await next();
    }
}