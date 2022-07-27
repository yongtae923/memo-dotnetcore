using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AblyAPI.Models.Data;
using AblyAPI.Models.Requests;
using AblyAPI.Models.Responses;
using AblyAPI.Services;
using Microsoft.EntityFrameworkCore;
using PhoneNumbers;
using Xunit;

namespace AblyAPITest.Services;

public class UserServiceTest
{
    private readonly DatabaseContext _database;
    private readonly IUserService _service;
    private readonly PhoneNumberUtil _phone;

    public UserServiceTest()
    {
        var options = new DbContextOptionsBuilder<DatabaseContext>()
            .EnableSensitiveDataLogging()
            .UseInMemoryDatabase(Ulid.NewUlid().ToString())
            .Options;
        _database = new DatabaseContext(options);
        _service = new UserService(_database);
        _phone = PhoneNumberUtil.GetInstance();
    }

    [Fact(DisplayName = "GetUserInformationAsync : 입력값에 해당하는 계정이 있으면 계정 정보를 포함한 Ok를 반환합니다")]
    public async void Does_GetUserInformationAsync_Works_Well()
    {
        // Let
        var account = new Account
        {
            Id = Ulid.NewUlid().ToString(),
            Name = Ulid.NewUlid().ToString(),
            Nickname = Ulid.NewUlid().ToString(),
            Phone = "01012345678",
            Email = "yongtae@a-bly.com",
            CreatedAt = DateTimeOffset.UtcNow
        };
        _database.Accounts.Add(account);
        await _database.SaveChangesAsync();
        
        // Do
        var response = await _service.GetUserInformationAsync(account.Id);
        
        // Check
        Assert.IsType<StatusResponse>(response);
        Assert.Equal(StatusType.Success, response.Status);
        Assert.IsType<UserInformationResponse>(response.Body);

        var userInformation = (UserInformationResponse) response.Body!;
        Assert.Equal(account.Name, userInformation.Name);
        Assert.Equal(account.Nickname, userInformation.Nickname);
        Assert.Equal(account.Phone, userInformation.Phone);
        Assert.Equal(account.Email, userInformation.Email);
        Assert.Equal(account.CreatedAt, userInformation.CreatedAt);
    }

    [Fact(DisplayName = "GetUserInformationAsync : 입력값에 해당하는 계정이 없으면 Unauthorized를 반환합니다")]
    public async void Does_GetUserInformationAsync_Return_Unauthorized_When_Account_Is_Not_Found()
    {
        // Do
        var response = await _service.GetUserInformationAsync(Ulid.NewUlid().ToString());

        // Check
        Assert.IsType<StatusResponse>(response);
        Assert.Equal(StatusType.Unauthorized, response.Status);
        Assert.Null(response.Body);
    }
    
    [Fact(DisplayName = "ChangePasswordAsync: 입력한 계정이 비밀번호를 변경하고 인증코드를 만료시키고 Ok를 반환합니다")]
    public async void Does_ChangePasswordAsync_Change_Password_Well()
    {
        // Let
        const string newPassword = "yongtae@ably!";
        
        var account = await TestRegister(TestRegisterRequestModel);
        
        var codeBefore = new VerificationCode(account.Phone) {VerifiesAt = DateTimeOffset.UtcNow};
        _database.VerificationCodes.Add(codeBefore);
        await _database.SaveChangesAsync();
        
        // Do
        var response = await _service.ChangePasswordAsync(account.Id, newPassword);
        
        // Check
        Assert.IsType<StatusResponse>(response);
        Assert.Equal(StatusType.Success, response.Status);
        Assert.Null(response.Body);

        var codeAfter = await _database.VerificationCodes.SingleOrDefaultAsync(code => code.Id == codeBefore.Id);
        Assert.NotNull(codeAfter);
        Assert.True(codeAfter!.ExpiresAt < DateTimeOffset.UtcNow);

        var credentialAfter =
            await _database.Credentials.SingleOrDefaultAsync(credential => credential.AccountId == account.Id);
        Assert.NotNull(credentialAfter);
        Assert.True(credentialAfter!.VerifyPassword(newPassword));
    }

    [Fact(DisplayName = "ChangePasswordAsync: 입력한 계정을 찾을 수 없으면 Unauthorized를 반환합니다")]
    public async void Does_ChangePasswordAsync_Return_Unauthorized_When_Account_Is_Not_Found()
    {
        // Let
        const string newPassword = "yongtae@ably!";

        // Do
        var response = await _service.ChangePasswordAsync(Ulid.NewUlid().ToString(), newPassword);

        // Check
        Assert.IsType<StatusResponse>(response);
        Assert.Equal(StatusType.Unauthorized, response.Status);
        Assert.Null(response.Body);
    }

    [Fact(DisplayName = "ChangePasswordAsync: 입력한 계정의 자격을 찾을 수 없으면 Unauthorized를 반환합니다")]
    public async void Does_ChangePasswordAsync_Return_Unauthorized_When_Credential_Is_Not_Found()
    {
        // Let
        const string newPassword = "yongtae@ably!";
        var account = new Account
        {
            Id = Ulid.NewUlid().ToString(),
            Name = "Yongtae Kim",
            Nickname = "Ably-dev",
            Phone = "01012345678",
            Email = "yongtae@a-bly.com",
            Credentials = new List<Credential>()
        };
        _database.Accounts.Add(account);

        // Do
        var response = await _service.ChangePasswordAsync(account.Id, newPassword);

        // Check
        Assert.IsType<StatusResponse>(response);
        Assert.Equal(StatusType.Unauthorized, response.Status);
        Assert.Null(response.Body);
    }

    [Fact(DisplayName = "ChangePasswordAsync: 활성화된 인증코드가 없으면 Forbidden을 반환합니다.")]
    public async void Does_ChangePasswordAsync_Return_Forbidden_When_There_Is_No_Active_Verification_Code()
    {
        // Let
        const string newPassword = "yongtae@ably!";

        var account = await TestRegister(TestRegisterRequestModel);

        // Do
        var response = await _service.ChangePasswordAsync(account.Id, newPassword);

        // Check
        Assert.IsType<StatusResponse>(response);
        Assert.Equal(StatusType.Forbidden, response.Status);
        Assert.Null(response.Body);
    }

    private string ParseToFormat(string phone) => _phone.Format(_phone.Parse(phone, "KR"), PhoneNumberFormat.E164);

    private static RegisterRequestModel TestRegisterRequestModel => new()
    {
        Email = "yongtae@a-bly.com",
        Password = "yongtae@ably!",
        Name = "Yongtae Kim",
        Nickname = "Ably-dev",
        Phone = "01012345678"
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