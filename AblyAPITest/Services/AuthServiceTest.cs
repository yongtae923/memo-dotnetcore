using System;
using System.Linq;
using AblyAPI.Models.Data;
using AblyAPI.Models.DTO;
using AblyAPI.Models.Requests;
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

    [Fact(DisplayName = "RequestVerificationCodeAsync: 전화번호를 입력받고 인증번호를 저장한 후 Ok response를 반환합니다")]
    public async void Does_RequestVerificationCodeAsync_Return_Ok_Verification_Code_When_Phone_Number_Is_Right()
    {
        // Let
        var model = new PhoneNumberRequestModel {Phone = "01012345678"};
        
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

    [Fact(DisplayName = "RequestVerificationCodeAsync: 틀린 형식의 전화번호를 입력받으면 BadRequest response를 반환합니다")]
    public async void Does_RequestVerificationCodeAsync_Return_BadRequest_When_Phone_Number_Is_Wrong()
    {
        // Let
        var model = new PhoneNumberRequestModel {Phone = "WrongNumber"};
        
        // Do
        var response = await _service.RequestVerificationCodeAsync(model);
        
        // Check
        Assert.IsType<StatusResponse>(response);
        Assert.Equal(StatusType.BadRequest, response.Status);
        Assert.Null(response.Body);
    }

    [Fact(DisplayName = "VerifyCodeAsync: 성공하면 입력값에 해당하는 모든 활성 인증코드를 인증 처리합니다")]
    public async void Does_VerifyCodeAsync_Verify_Right_Codes_Well()
    {
        // Let
        var model = new PhoneNumberRequestModel {Phone = "01012345678"};
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

    [Fact(DisplayName = "VerifyCodeAsync: 틀린 형식의 전화번호를 입력받으면 BadRequest response를 반환합니다")]
    public async void Does_VerifyCodeAsync_Return_BadRequest_When_Phone_Number_Is_Wrong()
    {
        // Let
        var model = new PhoneNumberRequestModel {Phone = "01012345678"};
        model.Phone = "Wrong Number";
        
        // Do
        var response = await _service.VerifyCodeAsync("123456", model);

        // Check
        Assert.IsType<StatusResponse>(response);
        Assert.Equal(StatusType.BadRequest, response.Status);
        Assert.Null(response.Body);
    }

    [Fact(DisplayName = "VerifyCodeAsync: 입력값에 해당하는 인증코드가 없으면 NotFound response를 반환합니다")]
    public async void Does_VerifyCodeAsync_Return_NotFound_When_There_Is_No_Right_Verification_Code()
    {
        // Let
        var model = new PhoneNumberRequestModel {Phone = "01012345678"};

        // Do
        var response = await _service.VerifyCodeAsync("123456", model);

        // Check
        Assert.IsType<StatusResponse>(response);
        Assert.Equal(StatusType.NotFound, response.Status);
        Assert.Null(response.Body);
    }

    [Fact(DisplayName = "VerifyCodeAsync: 입력값에 해당하는 인증코드가 있지만 모두 만료되어 있으면 RequestTimeout response를 반환합니다")]
    public async void Does_VerifyCodeAsync_Return_RequestTimeout_Response_When_Every_Right_Verification_Code_Is_Outdated()
    {
        // Let
        var model = new PhoneNumberRequestModel {Phone = "01012345678"};
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

    private string ParseToFormat(string phone) => _phone.Format(_phone.Parse(phone, "KR"), PhoneNumberFormat.E164);
}