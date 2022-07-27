using AblyAPI.Models.Responses;
using Microsoft.EntityFrameworkCore;

namespace AblyAPI.Services;

public interface IUserService
{
    /// <summary>
    /// 입력값에 알맞은 사용자 정보를 반환합니다.
    /// </summary>
    /// <param name="accountId">계정 아이디</param>
    /// <returns>접근토큰이 없거나 찾을 수 없으면 Unauthorized, 접근토큰으로 찾은 계정이 입력값과 다르면 Forbidden, 성공하면 Ok와 사용자 정보를 반환합니다.</returns>
    Task<StatusResponse> GetUserInformationAsync(string accountId);
}

public class UserService : IUserService
{
    private readonly DatabaseContext _database;

    public UserService(DatabaseContext database)
    {
        _database = database;
    }

    public async Task<StatusResponse> GetUserInformationAsync(string accountId)
    {
        var account = await _database.Accounts.FirstOrDefaultAsync(account => account.Id == accountId);
        
        return account != null
            ? new StatusResponse(StatusType.Success, new UserInformationResponse(account))
            : new StatusResponse(StatusType.Unauthorized);
    }
}