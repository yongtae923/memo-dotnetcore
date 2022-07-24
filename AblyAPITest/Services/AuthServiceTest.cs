using System;
using System.Linq;
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

    public AuthServiceTest()
    {
        var options = new DbContextOptionsBuilder<DatabaseContext>()
            .EnableSensitiveDataLogging()
            .UseInMemoryDatabase(Ulid.NewUlid().ToString())
            .Options;
        _database = new DatabaseContext(options);
        _service = new AuthService(_database);
    }

    [Fact(DisplayName = "RequestVerificationCodeAsync: 전화번호를 입력받고 인증번호를 저장한 후 Ok response를 반환합니다")]
    public async void Does_RequestVerificationCodeAsync_Return_Ok_Verification_Code_When_Phone_Number_Is_Right()
    {
        // Let
        var model = new VerificationCodeRequestModel {Phone = "01012345678"};
        
        // Do
        var response = await _service.RequestVerificationCodeAsync(model);
        
        // Check
        Assert.IsType<StatusResponse>(response);
        Assert.Equal(StatusType.Success, response.Status);
        Assert.IsType<string>(response.Body);
        
        var code = _database.VerificationCodes.SingleOrDefault();
        Assert.NotNull(code);
        Assert.IsType<string>(code!.Id);
        
        var phoneUtil = PhoneNumberUtil.GetInstance();
        var phoneString = phoneUtil.Format(phoneUtil.Parse(model.Phone, "KR"), PhoneNumberFormat.E164);
        Assert.Equal(code.Phone, phoneString);
        
        Assert.Equal(code.Code, response.Body);
        Assert.Null(code.VerifiesAt);
        Assert.IsType<DateTimeOffset>(code.ExpiresAt);
    }

    [Fact(DisplayName = "RequestVerificationCodeAsync: 틀린 형식의 전화번호를 입력받으면 BadRequest response를 반환합니다")]
    public async void Does_RequestVerificationCodeAsync_Return_BadRequest_When_Phone_Number_Is_Wrong()
    {
        // Let
        var model = new VerificationCodeRequestModel {Phone = "WrongNumber"};
        
        // Do
        var response = await _service.RequestVerificationCodeAsync(model);
        
        // Check
        Assert.IsType<StatusResponse>(response);
        Assert.Equal(StatusType.BadRequest, response.Status);
        Assert.Null(response.Body);
    }
}