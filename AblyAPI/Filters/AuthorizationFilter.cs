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
        if (!AuthenticationHeaderValue.TryParse(context.HttpContext.Request.Headers.Authorization.ToString(),
                out var value)) ReturnUnauthorized(context, next);

        if (value?.Parameter is null) ReturnUnauthorized(context, next);

        var database = context.HttpContext.RequestServices.GetService<DatabaseContext>();
        if (database is null) ReturnUnauthorized(context, next);

        var token = await database!.AccessTokens.SingleOrDefaultAsync(token => token.Token == value!.Parameter);
        if (token is null) ReturnUnauthorized(context, next);
        
        ((UserController) context.Controller).AccountId = token!.AccountId;
        await next();
    }

    private static async void ReturnUnauthorized(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        context.Result = new UnauthorizedObjectResult(context.HttpContext.Request.Path);
        await next();
    }
}