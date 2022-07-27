using AblyAPI.Models.Responses;
using Microsoft.EntityFrameworkCore;

namespace AblyAPI.Services;

public interface IUserService
{
    /// <summary>
    /// 입력값에 알맞은 사용자 정보를 반환합니다.
    /// </summary>
    /// <param name="accountId">계정 아이디</param>
    /// <returns>계정을 찾을 수 없으면 Unauthorized, 성공하면 Ok와 사용자 정보를 반환합니다.</returns>
    Task<StatusResponse> GetUserInformationAsync(string accountId);

    /// <summary>
    /// 입력값에 알맞게 비밀번호를 변경합니다.
    /// </summary>
    /// <param name="accountId">계정 아이디</param>
    /// <param name="newPassword">바꾸려는 비밀번호</param>
    /// <returns>계정을 찾을 수 없으면 Unauthorized, 성공하면 Ok를 반환합니다.</returns>
    Task<StatusResponse> ChangePasswordAsync(string accountId, string newPassword);
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

    public async Task<StatusResponse> ChangePasswordAsync(string accountId, string newPassword)
    {
        throw new NotImplementedException();
    }
}