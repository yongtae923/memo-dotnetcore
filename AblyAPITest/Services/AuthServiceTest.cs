using System;
using System.Linq;
using AblyAPI.Models.Requests;
using AblyAPI.Services;
using Microsoft.EntityFrameworkCore;
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

    [Fact(DisplayName = "RequestVerificationCodeAsync: 전화번호를 입력받고 인증번호를 저장한 후 반환합니다")]
    public async void Does_RequestVerificationCodeAsync_Return_Verification_Code_Well()
    {
        // Let
        var model = new VerificationCodeRequestModel {Phone = "01012345678"};
        
        // Do
        var response = _service.RequestVerificationCodeAsync(model);
        
        // Check
        Assert.IsType<string>(response);
        
        var code = _database.VerificationCodes.SingleOrDefault();
        Assert.NotNull(code);
        Assert.IsType<string>(code!.Id);
        Assert.Equal(code.Phone, model.Phone);
        Assert.Equal(code.Code, response);
        Assert.Null(code.VerifiesAt);
        Assert.IsType<DateTimeOffset>(code.ExpiresAt);
    }
}