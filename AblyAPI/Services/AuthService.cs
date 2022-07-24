using AblyAPI.Models.Data;
using AblyAPI.Models.DTO;
using AblyAPI.Models.Requests;
using PhoneNumbers;

namespace AblyAPI.Services;

public interface IAuthService
{
    /// <summary>
    /// 입력받은 model에서 전화번호를 받아 인증번호를 만들고 저장한 뒤 인증번호를 반환합니다.
    /// </summary>
    /// <param name="model">전화번호</param>
    /// <returns>인증번호</returns>
    Task<StatusResponse> RequestVerificationCodeAsync(VerificationCodeRequestModel model);
}

public class AuthService : IAuthService
{
    private readonly DatabaseContext _database;
    private readonly PhoneNumberUtil _phone;

    public AuthService(DatabaseContext database)
    {
        _database = database;
        _phone = PhoneNumberUtil.GetInstance();
    }

    public async Task<StatusResponse> RequestVerificationCodeAsync(VerificationCodeRequestModel model)
    {
        var phoneString = model.Phone;
        
        if (!PhoneNumberUtil.IsViablePhoneNumber(phoneString)) return new StatusResponse(StatusType.BadRequest);
        
        var code = new VerificationCode(_phone.Format(_phone.Parse(phoneString, "KR"), PhoneNumberFormat.E164));
        
        _database.VerificationCodes.Add(code);
        await _database.SaveChangesAsync();
        
        return new StatusResponse(StatusType.Success, code.Code);
    }
}