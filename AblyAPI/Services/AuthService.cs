using System.ComponentModel.DataAnnotations;
using AblyAPI.Models.Data;
using AblyAPI.Models.Requests;
using AblyAPI.Models.Responses;
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

    /// <summary>
    /// 입력값이 올바르면 회원가입 처리합니다.
    /// </summary>
    /// <param name="model">회원가입 입력모델: 이메일, 비밀번호, 이름, 닉네임, 전화번호</param>
    /// <returns>전화번호나 이메일이 올바르지 않으면 BadRequest, 이미 전화번호나 이메일이 겹치는 계정이 있으면 Conflict, 활성된 인증코드가 없으면 Forbidden, 성공하면 모든 활성 인증코드를 만료시키고 계정을 저장하고 Ok를 반환합니다.</returns>
    Task<StatusResponse> RegisterAsync(RegisterRequestModel model);

    /// <summary>
    /// 입력값이 올바르면 로그인 처리하고 접근토큰을 반환합니다.
    /// </summary>
    /// <param name="model">로그인 입력모델: 아이디, 비밀번호</param>
    /// <returns>아이디가 올바르지 않으면 BadRequest, 아이디로 계정을 찾을 수 없으면 Unauthorized, 비밀번호가 올바르지 않으면 Forbidden, 성공하면 접근토큰을 생성하고 Ok와 접근토큰을 반환합니다.</returns>
    Task<StatusResponse> LoginAsync(LoginRequestModel model);
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
        // 전화번호가 올바른 형식인지 확인합니다.
        var phoneString = model.Phone;
        if (!PhoneNumberUtil.IsViablePhoneNumber(phoneString)) return new StatusResponse(StatusType.BadRequest);
        
        // 전화번호에 해당하는 인증코드를 생성하고 저장하고 반환합니다.
        var code = new VerificationCode(ParseToFormat(phoneString));
        
        _database.VerificationCodes.Add(code);
        await _database.SaveChangesAsync();
        
        return new StatusResponse(StatusType.Success, code.Code);
    }

    public async Task<StatusResponse> VerifyCodeAsync(string verifyingCode, PhoneNumberRequestModel model)
    {
        // 전화번호가 올바른 형식인지 확인합니다.
        var phoneString = model.Phone;
        if (!PhoneNumberUtil.IsViablePhoneNumber(phoneString)) return new StatusResponse(StatusType.BadRequest);

        // 전화번호에 해당하는 인증코드를 찾습니다.
        var matchedCodes = _database.VerificationCodes.Where(code =>
            code.Code == verifyingCode && code.Phone == ParseToFormat(phoneString));
        if (!matchedCodes.Any()) return new StatusResponse(StatusType.NotFound);

        // 인증코드가 만료되지 않았는지 확인합니다.
        var activeCodes = matchedCodes.Where(code => code.ExpiresAt > DateTimeOffset.UtcNow);
        if (!activeCodes.Any()) return new StatusResponse(StatusType.RequestTimeout);

        // 인증코드를 활성화합니다.
        activeCodes.ToList().ForEach(code => code.VerifiesAt = DateTimeOffset.UtcNow);
        await _database.SaveChangesAsync();

        return new StatusResponse(StatusType.Success);
    }

    public async Task<StatusResponse> RegisterAsync(RegisterRequestModel model)
    {
        var phoneString = model.Phone;
        var emailString = model.Email;

        // 전화번호와 이메일이 각각 올바른 형식인지 확인합니다.
        var invalidPhone = !PhoneNumberUtil.IsViablePhoneNumber(phoneString);
        var invalidEmail = !new EmailAddressAttribute().IsValid(emailString);
        if (invalidPhone || invalidEmail) return new StatusResponse(StatusType.BadRequest, new RegisterErrorResponse
            {Phone = invalidPhone ? phoneString : null, Email = invalidEmail ? emailString : null});

        var parsedPhone = ParseToFormat(phoneString);
        
        // 이미 가입된 계정에서 전화번호나 이메일이 겹치는지 확인합니다. 
        var existingPhone = await _database.Accounts.AnyAsync(account => account.Phone == parsedPhone);
        var existingEmail = await _database.Accounts.AnyAsync(account => account.Email == emailString);
        if (existingPhone || existingEmail) return new StatusResponse(StatusType.Conflict, new RegisterErrorResponse
            {Phone = existingPhone ? phoneString : null, Email = existingEmail ? emailString : null});

        // 만료되지 않고 활성화 되어있는 인증코드를 찾습니다.
        var validCodes = _database.VerificationCodes.Where(code =>
            code.Phone == parsedPhone && code.VerifiesAt < DateTimeOffset.UtcNow &&
            code.ExpiresAt > DateTimeOffset.UtcNow).ToList();
        if (validCodes.Count == 0) return new StatusResponse(StatusType.Forbidden);
        
        // 모든 활성 인증코드를 만료시킵니다.
        validCodes.ForEach(code => code.ExpiresAt = DateTimeOffset.UtcNow);

        // 계정과 엑세스토큰을 생성하고 반환합니다.
        var account = model.ToAccount(parsedPhone);
        var accessToken = new AccessToken(account);
        
        account.AccessTokens.Add(accessToken);
        account.Credentials.Add(new Credential(account, model.Password));
        _database.Accounts.Add(account);
        await _database.SaveChangesAsync();

        // 엑세스토큰을 반환합니다.
        return new StatusResponse(StatusType.Success, new AccessTokenResponse(accessToken));
    }

    public async Task<StatusResponse> LoginAsync(LoginRequestModel model)
    {
        Account? account;
        
        // 아이디가 가입된 계정의 이메일이나 전화번호인지 확인합니다.
        if (new EmailAddressAttribute().IsValid(model.Id))
        {
            account = await _database.Accounts.SingleOrDefaultAsync(a => a.Email == model.Id);
        }
        else if (PhoneNumberUtil.IsViablePhoneNumber(model.Id))
        {
            account = await _database.Accounts.SingleOrDefaultAsync(a => a.Phone == ParseToFormat(model.Id));
        }
        else return new StatusResponse(StatusType.BadRequest);

        if (account is null) return new StatusResponse(StatusType.Unauthorized);

        // 계정의 자격을 확인합니다.
        var credential = account.Credentials.SingleOrDefault(a => a.Provider == Providers.Self);
        if (credential is null) return new StatusResponse(StatusType.Unauthorized);
        
        // 비밀번호를 확인합니다.
        if (!credential.VerifyPassword(model.Password)) return new StatusResponse(StatusType.Forbidden);

        // 엑세스 토큰을 생성하고 반환합니다.
        var accessToken = new AccessToken(account);
        _database.AccessTokens.Add(accessToken);
        await _database.SaveChangesAsync();
        
        return new StatusResponse(StatusType.Success, new AccessTokenResponse(accessToken));
    }

    private string ParseToFormat(string phone) => _phone.Format(_phone.Parse(phone, "KR"), PhoneNumberFormat.E164);
}