using AblyAPI.Models.Responses;
using Microsoft.EntityFrameworkCore;

namespace AblyAPI.Services;

public interface IUserService
{
    /// <summary>
    /// 입력값에 알맞은 사용자 정보를 반환합니다.
    /// </summary>
    /// <param name="accountId">계정 아이디</param>
    /// <param name="accessToken">접근토큰</param>
    /// <returns>접근토큰이 없거나 찾을 수 없으면 Unauthorized, 접근토큰으로 찾은 계정이 입력값과 다르면 Forbidden, 성공하면 Ok와 사용자 정보를 반환합니다.</returns>
    Task<StatusResponse> GetUserInformationAsync(string accountId, string accessToken);
}

public class UserService : IUserService
{
    private readonly DatabaseContext _database;

    public UserService(DatabaseContext database)
    {
        _database = database;
    }

    public async Task<StatusResponse> GetUserInformationAsync(string accountId, string accessToken)
    {
        var token = await _database.AccessTokens.SingleOrDefaultAsync(token => token.Token == accessToken);
        if (token is null) return new StatusResponse(StatusType.Unauthorized);

        return token.AccountId == accountId
            ? new StatusResponse(StatusType.Success, new UserInformationResponse(token.Account))
            : new StatusResponse(StatusType.Forbidden);
    }
}