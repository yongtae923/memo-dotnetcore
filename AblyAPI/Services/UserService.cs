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
    /// <returns>계정을 찾을 수 없으면 Unauthorized, 활성된 인증번호를 찾지 못했으면 Forbidden, 성공하면 Ok를 반환합니다.</returns>
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
        // 아이디에 해당하는 계정을 찾습니다.
        var account = await _database.Accounts.FirstOrDefaultAsync(account => account.Id == accountId);
        
        // 찾았으면 계정 정보를 반환합니다.
        return account != null
            ? new StatusResponse(StatusType.Success, new UserInformationResponse(account))
            : new StatusResponse(StatusType.Unauthorized);
    }

    public async Task<StatusResponse> ChangePasswordAsync(string accountId, string newPassword)
    {
        // 아이디에 해당하는 계정과 자격을 찾습니다.
        var account = await _database.Accounts.FirstOrDefaultAsync(account => account.Id == accountId);
        if (account is null) return new StatusResponse(StatusType.Unauthorized);
        var credential = account.Credentials.FirstOrDefault();
        if (credential is null) return new StatusResponse(StatusType.Unauthorized);
        
        // 만료되지 않고 활성화된 인증코드가 있는지 확인합니다.
        var validCodes = _database.VerificationCodes.Where(code =>
            code.Phone == account.Phone && code.VerifiesAt < DateTimeOffset.UtcNow &&
            code.ExpiresAt > DateTimeOffset.UtcNow).ToList();
        if (validCodes.Count == 0) return new StatusResponse(StatusType.Forbidden);

        // 모든 활성 인증코드를 만료시킵니다.
        validCodes.ForEach(code => code.ExpiresAt = DateTimeOffset.UtcNow);

        // 비밀번호를 변경합니다.
        credential.Password = newPassword;
        await _database.SaveChangesAsync();

        return new StatusResponse(StatusType.Success);
    }
}