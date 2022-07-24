using AblyAPI.Models.Data;
using AblyAPI.Models.Requests;

namespace AblyAPI.Services;

public interface IAuthService
{
    /// <summary>
    /// 입력받은 model에서 전화번호를 받아 인증번호를 만들고 저장한 뒤 인증번호를 반환합니다.
    /// </summary>
    /// <param name="model">전화번호</param>
    /// <returns>인증번호</returns>
    string RequestVerificationCodeAsync(VerificationCodeRequestModel model);
}

public class AuthService : IAuthService
{
    private readonly DatabaseContext _database;

    public AuthService(DatabaseContext database)
    {
        _database = database;
    }

    public string RequestVerificationCodeAsync(VerificationCodeRequestModel model)
    {
        var code = new VerificationCode(model.Phone);
        _database.VerificationCodes.Add(code);
        return code.Code;
    }
}