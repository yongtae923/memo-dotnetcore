using System.ComponentModel.DataAnnotations;
using AblyAPI.Models.Data;
using AblyAPI.Models.DTO;
using AblyAPI.Models.Requests;
using Microsoft.EntityFrameworkCore;
using PhoneNumbers;

namespace AblyAPI.Services;

public interface IAuthService
{
    /// <summary>
    /// 입력받은 model에서 전화번호를 받아 인증번호를 만들고 저장한 뒤 인증번호를 반환합니다.
    /// </summary>
    /// <param name="model">전화번호</param>
    /// <returns>전화번호가 올바르지 않으면 BadRequest, 성공하면 Ok와 인증코드를 반환합니다.</returns>
    Task<StatusResponse> RequestVerificationCodeAsync(PhoneNumberRequestModel model);

    /// <summary>
    /// 입력받은 코드에 맞는 활성 인증코드가 있으면 그 코드에 해당하는 모든 활성 인증코드를 인증 처리합니다.
    /// </summary>
    /// <param name="verifyingCode">인증코드</param>
    /// <param name="model">전화번호</param>
    /// <returns>전화번호가 올바르지 않으면 BadRequest, 해당하는 인증코드가 없으면 NotFound, 인증코드가 있지만 만료되었다면 RequestTimeout, 성공하면 Ok를 반환합니다.</returns>
    Task<StatusResponse> VerifyCodeAsync(string verifyingCode, PhoneNumberRequestModel model);
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

    public async Task<StatusResponse> RequestVerificationCodeAsync(PhoneNumberRequestModel model)
    {
        var phoneString = model.Phone;
        if (!PhoneNumberUtil.IsViablePhoneNumber(phoneString)) return new StatusResponse(StatusType.BadRequest);
        
        var code = new VerificationCode(ParseToFormat(phoneString));
        
        _database.VerificationCodes.Add(code);
        await _database.SaveChangesAsync();
        
        return new StatusResponse(StatusType.Success, code.Code);
    }

    public async Task<StatusResponse> VerifyCodeAsync(string verifyingCode, PhoneNumberRequestModel model)
    {
        var phoneString = model.Phone;
        if (!PhoneNumberUtil.IsViablePhoneNumber(phoneString)) return new StatusResponse(StatusType.BadRequest);

        var matchedCodes = _database.VerificationCodes.Where(code =>
            code.Code == verifyingCode && code.Phone == ParseToFormat(phoneString));
        if (!matchedCodes.Any()) return new StatusResponse(StatusType.NotFound);

        var activeCodes = matchedCodes.Where(code => code.ExpiresAt > DateTimeOffset.UtcNow);
        if (!activeCodes.Any()) return new StatusResponse(StatusType.RequestTimeout);

        activeCodes.ToList().ForEach(code => code.VerifiesAt = DateTimeOffset.UtcNow);
        await _database.SaveChangesAsync();

        return new StatusResponse(StatusType.Success);
    }

    public async Task<StatusResponse> RegisterAsync(RegisterRequestModel model)
    {
        var phoneString = model.Phone;
        var emailString = model.Email;

        var invalidPhone = !PhoneNumberUtil.IsViablePhoneNumber(phoneString);
        var invalidEmail = !new EmailAddressAttribute().IsValid(emailString);
        if (invalidPhone || invalidEmail || model.Password.Length < 6) return new StatusResponse(StatusType.BadRequest,
            new {Phone = invalidPhone ? phoneString : null, Email = invalidEmail ? emailString : null});

        var parsedPhone = ParseToFormat(phoneString);
        
        var existingPhone = await _database.Accounts.AnyAsync(account => account.Phone == parsedPhone);
        var existingEmail = await _database.Accounts.AnyAsync(account => account.Email == emailString);
        if (existingPhone || existingEmail) return new StatusResponse(StatusType.Conflict,
            new {Phone = existingPhone ? phoneString : null, Email = existingEmail ? emailString : null});

        var validCodes = _database.VerificationCodes.Where(code =>
            code.Phone == parsedPhone && code.VerifiesAt < DateTimeOffset.UtcNow &&
            code.ExpiresAt > DateTimeOffset.UtcNow).ToList();
        if (validCodes.Count == 0) return new StatusResponse(StatusType.Forbidden);
        
        validCodes.ForEach(code => code.ExpiresAt = DateTimeOffset.UtcNow);
        
        _database.Accounts.Add(model.ToAccount(parsedPhone));
        await _database.SaveChangesAsync();

        return new StatusResponse(StatusType.Success);
    }

    private string ParseToFormat(string phone) => _phone.Format(_phone.Parse(phone, "KR"), PhoneNumberFormat.E164);
}