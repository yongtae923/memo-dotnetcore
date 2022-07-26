using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AblyAPI.Models.Data;
using AblyAPI.Models.Requests;
using AblyAPI.Models.Responses;
using AblyAPI.Services;
using Microsoft.EntityFrameworkCore;
using PhoneNumbers;
using Xunit;

namespace AblyAPITest.Services;

public class AuthServiceTest
{
    private readonly DatabaseContext _database;
    private readonly IAuthService _service;
    private readonly PhoneNumberUtil _phone;

    public AuthServiceTest()
    {
        var options = new DbContextOptionsBuilder<DatabaseContext>()
            .EnableSensitiveDataLogging()
            .UseInMemoryDatabase(Ulid.NewUlid().ToString())
            .Options;
        _database = new DatabaseContext(options);
        _service = new AuthService(_database);
        _phone = PhoneNumberUtil.GetInstance();
    }

    [Fact(DisplayName = "RequestVerificationCodeAsync: 전화번호를 입력받고 인증번호를 저장한 후 Ok를 반환합니다")]
    public async void Does_RequestVerificationCodeAsync_Return_Ok_Verification_Code_When_Phone_Number_Is_Right()
    {
        // Let
        var model = TestPhoneNumberRequestModel;
        
        // Do
        var response = await _service.RequestVerificationCodeAsync(model);
        
        // Check
        Assert.IsType<StatusResponse>(response);
        Assert.Equal(StatusType.Success, response.Status);
        Assert.IsType<string>(response.Body);
        
        var code = _database.VerificationCodes.SingleOrDefault();
        Assert.NotNull(code);
        Assert.IsType<string>(code!.Id);
        Assert.Equal(code.Phone, ParseToFormat(model.Phone));
        Assert.Equal(code.Code, response.Body);
        Assert.Null(code.VerifiesAt);
        Assert.IsType<DateTimeOffset>(code.ExpiresAt);
    }

    [Fact(DisplayName = "RequestVerificationCodeAsync: 틀린 형식의 전화번호를 입력받으면 BadRequest를 반환합니다")]
    public async void Does_RequestVerificationCodeAsync_Return_BadRequest_When_Phone_Number_Is_Wrong()
    {
        // Let
        var model = TestPhoneNumberRequestModel;
        model.Phone = "WrongNumber";
        
        // Do
        var response = await _service.RequestVerificationCodeAsync(model);
        
        // Check
        Assert.IsType<StatusResponse>(response);
        Assert.Equal(StatusType.BadRequest, response.Status);
        Assert.Null(response.Body);
    }

    [Fact(DisplayName = "VerifyCodeAsync: 성공하면 입력값에 해당하는 모든 활성 인증코드를 인증 처리하고 Ok를 반환합니다")]
    public async void Does_VerifyCodeAsync_Verify_Right_Codes_Well()
    {
        // Let
        var model = TestPhoneNumberRequestModel;
        var savedCode = new VerificationCode(ParseToFormat(model.Phone));
        
        _database.VerificationCodes.Add(savedCode);
        await _database.SaveChangesAsync();
        
        // Do
        var response = await _service.VerifyCodeAsync(savedCode.Code, model);

        // Check
        Assert.IsType<StatusResponse>(response);
        Assert.Equal(StatusType.Success, response.Status);
        Assert.Null(response.Body);

        var code = _database.VerificationCodes.SingleOrDefault();
        Assert.NotNull(code);
        Assert.IsType<string>(code!.Id);
        Assert.NotNull(code.VerifiesAt);
        Assert.IsType<DateTimeOffset>(code.VerifiesAt);
        Assert.True(code.ExpiresAt > DateTimeOffset.Now);
    }

    [Fact(DisplayName = "VerifyCodeAsync: 틀린 형식의 전화번호를 입력받으면 BadRequest를 반환합니다")]
    public async void Does_VerifyCodeAsync_Return_BadRequest_When_Phone_Number_Is_Wrong()
    {
        // Let
        var model = TestPhoneNumberRequestModel;
        model.Phone = "Wrong Number";
        
        // Do
        var response = await _service.VerifyCodeAsync("123456", model);

        // Check
        Assert.IsType<StatusResponse>(response);
        Assert.Equal(StatusType.BadRequest, response.Status);
        Assert.Null(response.Body);
    }

    [Fact(DisplayName = "VerifyCodeAsync: 입력값에 해당하는 인증코드가 없으면 NotFound를 반환합니다")]
    public async void Does_VerifyCodeAsync_Return_NotFound_When_There_Is_No_Right_Verification_Code()
    {
        // Let
        var model = TestPhoneNumberRequestModel;

        // Do
        var response = await _service.VerifyCodeAsync("123456", model);

        // Check
        Assert.IsType<StatusResponse>(response);
        Assert.Equal(StatusType.NotFound, response.Status);
        Assert.Null(response.Body);
    }

    [Fact(DisplayName = "VerifyCodeAsync: 입력값에 해당하는 인증코드가 있지만 모두 만료되어 있으면 RequestTimeout를 반환합니다")]
    public async void Does_VerifyCodeAsync_Return_RequestTimeout_Response_When_Every_Right_Verification_Code_Is_Outdated()
    {
        // Let
        var model = TestPhoneNumberRequestModel;
        var outdatedCode = new VerificationCode(ParseToFormat(model.Phone))
        {
            Code = "123456",
            ExpiresAt = DateTimeOffset.MinValue
        };
        
        _database.VerificationCodes.Add(outdatedCode);
        await _database.SaveChangesAsync();

        // Do
        var response = await _service.VerifyCodeAsync(outdatedCode.Code, model);

        // Check
        Assert.IsType<StatusResponse>(response);
        Assert.Equal(StatusType.RequestTimeout, response.Status);
        Assert.Null(response.Body);
    }

    [Fact(DisplayName = "RegisterAsync: 입력값이 올바르면 모든 활성 인증코드를 만료시키고 계정을 저장한 뒤 Ok와 접근토큰을 반환합니다")]
    public async void Does_Register_Work_Well_When_Input_Is_Right()
    {
        // Let
        var model = TestRegisterRequestModel;
        var phone = ParseToFormat(model.Phone);
        _database.VerificationCodes.Add(
            new VerificationCode(phone) {VerifiesAt = DateTimeOffset.UtcNow});
        await _database.SaveChangesAsync();

        // Do
        var response = await _service.RegisterAsync(model);
        
        // Check
        Assert.IsType<StatusResponse>(response);
        Assert.Equal(StatusType.Success, response.Status);
        Assert.Null(response.Body);
        
        Assert.True(_database.VerificationCodes.First().ExpiresAt < DateTimeOffset.UtcNow);
        var account = await _database.Accounts.FirstOrDefaultAsync();
        Assert.NotNull(account);
        Assert.IsType<string>(account!.Id);
        Assert.Equal(model.ToAccount(phone).Name, account.Name);
        Assert.Equal(model.ToAccount(phone).Nickname, account.Nickname);
        Assert.Equal(phone, account.Phone);
        Assert.Equal(model.ToAccount(phone).Email, account.Email);
        Assert.IsType<DateTimeOffset>(account.CreatedAt);
    }

    [Fact(DisplayName = "RegisterAsync: 이메일이 올바른 형식이 아니면 BadRequest를 반환합니다")]
    public async void Does_Register_Return_BadRequest_When_Email_Is_Not_Right()
    {
        // Let
        var model = TestRegisterRequestModel;
        model.Email = "Wrong email";

        // Do
        var response = await _service.RegisterAsync(model);

        // Check
        Assert.IsType<StatusResponse>(response);
        Assert.Equal(StatusType.BadRequest, response.Status);
        Assert.IsType<RegisterErrorResponse>(response.Body);

        var errorResponse = (RegisterErrorResponse) response.Body!;
        Assert.Null(errorResponse.Phone);
        Assert.Equal(model.Email, errorResponse.Email);
    }

    [Fact(DisplayName = "RegisterAsync: 전화번호가 올바른 형식이 아니면 BadRequest를 반환합니다")]
    public async void Does_Register_Return_BadRequest_When_Phone_Is_Not_Right()
    {
        // Let
        var model = TestRegisterRequestModel;
        model.Phone = "Wrong phone number";

        // Do
        var response = await _service.RegisterAsync(model);

        // Check
        Assert.IsType<StatusResponse>(response);
        Assert.Equal(StatusType.BadRequest, response.Status);
        Assert.IsType<RegisterErrorResponse>(response.Body);

        var errorResponse = (RegisterErrorResponse) response.Body!;
        Assert.Equal(model.Phone, errorResponse.Phone);
        Assert.Null(errorResponse.Email);
    }

    [Fact(DisplayName = "RegisterAsync: 이메일이 겹치는 계정이 이미 가입되어 있으면 Conflict를 반환합니다")]
    public async void Does_Register_Return_Conflict_When_Another_Account_Has_Same_Email()
    {
        // Let
        var model = TestRegisterRequestModel;
        _database.Accounts.Add(new Account
        {
            Id = Ulid.NewUlid().ToString(),
            Name = Ulid.NewUlid().ToString(),
            Nickname = Ulid.NewUlid().ToString(),
            Phone = "01087654321",
            Email = model.Email,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await _database.SaveChangesAsync();

        // Do
        var response = await _service.RegisterAsync(model);

        // Check
        Assert.IsType<StatusResponse>(response);
        Assert.Equal(StatusType.Conflict, response.Status);
        Assert.IsType<RegisterErrorResponse>(response.Body);

        var errorResponse = (RegisterErrorResponse) response.Body!;
        Assert.Null(errorResponse.Phone);
        Assert.Equal(model.Email, errorResponse.Email);
    }

    [Fact(DisplayName = "RegisterAsync: 전화번호가 겹치는 계정이 이미 가입되어 있으면 Conflict를 반환합니다")]
    public async void Does_Register_Return_Conflict_When_Another_Account_Has_Same_Phone()
    {
        // Let
        var model = TestRegisterRequestModel;
        _database.Accounts.Add(new Account
        {
            Id = Ulid.NewUlid().ToString(),
            Name = Ulid.NewUlid().ToString(),
            Nickname = Ulid.NewUlid().ToString(),
            Phone = ParseToFormat(model.Phone),
            Email = "stranger@a-bly.com",
            CreatedAt = DateTimeOffset.UtcNow
        });
        await _database.SaveChangesAsync();

        // Do
        var response = await _service.RegisterAsync(model);

        // Check
        Assert.IsType<StatusResponse>(response);
        Assert.Equal(StatusType.Conflict, response.Status);
        Assert.IsType<RegisterErrorResponse>(response.Body);

        var errorResponse = (RegisterErrorResponse) response.Body!;
        Assert.Equal(model.Phone, errorResponse.Phone);
        Assert.Null(errorResponse.Email);
    }

    [Fact(DisplayName = "RegisterAsync: 활성화된 인증코드가 없으면 Forbidden을 반환합니다")]
    public async void Does_Register_Return_Forbidden_When_There_Is_No_Active_Verification_Code()
    {
        // Let
        var model = TestRegisterRequestModel;

        // Do
        var response = await _service.RegisterAsync(model);

        // Check
        Assert.IsType<StatusResponse>(response);
        Assert.Equal(StatusType.Forbidden, response.Status);
        Assert.Null(response.Body);
    }

    [Fact(DisplayName = "LoginAsync: 올바른 이메일을 입력 받으면 접근토큰을 생성하고 반환합니다")]
    public async void Does_LoginAsync_Works_Well_When_Email_Is_Right()
    {
        // Let
        var account = await TestRegister(TestRegisterRequestModel);
        var model = TestLoginRequestModel;
        model.Id = account.Email;

        // Do
        var response = await _service.LoginAsync(model);
        
        // Check
        Assert.IsType<StatusResponse>(response);
        Assert.Equal(StatusType.Success, response.Status);
        Assert.IsType<AccessTokenResponse>(response.Body);

        var token = await _database.AccessTokens.SingleOrDefaultAsync(token => token.AccountId == account.Id);
        Assert.NotNull(token);
        
        var tokenResponse = (AccessTokenResponse) response.Body!;
        Assert.Equal(token!.Token, tokenResponse.Token);
        Assert.Equal(token.RefreshToken, tokenResponse.RefreshToken);
        Assert.Equal(token.ExpiresAt, tokenResponse.ExpiresAt);
    }

    [Fact(DisplayName = "LoginAsync: 올바른 전화번호를 입력 받으면 접근토큰을 생성하고 반환합니다")]
    public async void Does_LoginAsync_Works_Well_When_Phone_Is_Right()
    {
        // Let
        var account = await TestRegister(TestRegisterRequestModel);
        var model = TestLoginRequestModel;
        model.Id = account.Phone;

        // Do
        var response = await _service.LoginAsync(model);

        // Check
        Assert.IsType<StatusResponse>(response);
        Assert.Equal(StatusType.Success, response.Status);
        Assert.IsType<AccessTokenResponse>(response.Body);

        var token = await _database.AccessTokens.SingleOrDefaultAsync(token => token.AccountId == account.Id);
        Assert.NotNull(token);

        var tokenResponse = (AccessTokenResponse) response.Body!;
        Assert.Equal(token!.Token, tokenResponse.Token);
        Assert.Equal(token.RefreshToken, tokenResponse.RefreshToken);
        Assert.Equal(token.ExpiresAt, tokenResponse.ExpiresAt);
    }

    [Fact(DisplayName = "LoginAsync: 올바르지 않은 형식의 아이디를 입력 받으면 BadRequest를 반환합니다")]
    public async void Does_LoginAsync_Return_BadRequest_When_Id_Is_Wrong_Format()
    {
        // Let
        var model = TestLoginRequestModel;
        model.Id = "Wrong format";

        // Do
        var response = await _service.LoginAsync(model);

        // Check
        Assert.IsType<StatusResponse>(response);
        Assert.Equal(StatusType.BadRequest, response.Status);
        Assert.Null(response.Body);
    }

    [Fact(DisplayName = "LoginAsync: 아이디에 해당하는 계정이 없으면 Unauthorized를 반환합니다")]
    public async void Does_LoginAsync_Return_Unauthorized_When_Account_Is_Not_Found()
    {
        // Let
        var model = TestLoginRequestModel;

        // Do
        var response = await _service.LoginAsync(model);

        // Check
        Assert.IsType<StatusResponse>(response);
        Assert.Equal(StatusType.Unauthorized, response.Status);
        Assert.Null(response.Body);
    }

    [Fact(DisplayName = "LoginAsync: 아이디에 해당하는 계정의 로그인 자격이 없으면 Unauthorized를 반환합니다")]
    public async void Does_LoginAsync_Return_Unauthorized_When_Accounts_Credential_Is_Not_Found()
    {
        // Let
        var account = await TestRegister(TestRegisterRequestModel);
        account.Credentials.RemoveAll(_ => true);
        await _database.SaveChangesAsync();
        var model = TestLoginRequestModel;

        // Do
        var response = await _service.LoginAsync(model);

        // Check
        Assert.IsType<StatusResponse>(response);
        Assert.Equal(StatusType.Unauthorized, response.Status);
        Assert.Null(response.Body);
    }

    [Fact(DisplayName = "LoginAsync: 입력값에 해당하는 계정은 있으나 비밀번호가 틀렸으면 Forbidden을 반환합니다")]
    public async void Does_LoginAsync_Return_Forbidden_When_Password_Is_Wrong()
    {
        // Let
        var account = await TestRegister(TestRegisterRequestModel);
        var model = TestLoginRequestModel;
        model.Password = "Wrong password";

        // Do
        var response = await _service.LoginAsync(model);

        // Check
        Assert.IsType<StatusResponse>(response);
        Assert.Equal(StatusType.Forbidden, response.Status);
        Assert.Null(response.Body);
    }
    
    private string ParseToFormat(string phone) => _phone.Format(_phone.Parse(phone, "KR"), PhoneNumberFormat.E164);

    private static PhoneNumberRequestModel TestPhoneNumberRequestModel => new() {Phone = "01012345678"};

    private static RegisterRequestModel TestRegisterRequestModel => new()
    {
        Email = "yongtae@a-bly.com",
        Password = "yongtae@ably!",
        Name = "Yongtae Kim",
        Nickname = "Ably-dev",
        Phone = "01012345678"
    };
    
    private static LoginRequestModel TestLoginRequestModel => new()
    {
        Id = "yongtae@a-bly.com",
        Password = "yongtae@ably!"
    };

    private async Task<Account> TestRegister(RegisterRequestModel model)
    {
        var account = new Account
        {
            Id = Ulid.NewUlid().ToString(),
            Name = model.Name,
            Nickname = model.Nickname,
            Phone = ParseToFormat(model.Phone),
            Email = model.Email,
            Credentials = new List<Credential>
            {
                new()
                {
                    Password = model.Password,
                    Provider = Providers.Self,
                    LastUpdatedAt = DateTimeOffset.UtcNow
                }
            }
        };
        _database.Accounts.Add(account);
        await _database.SaveChangesAsync();
        return account;
    }
}