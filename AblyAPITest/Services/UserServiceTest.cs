using System;
using AblyAPI.Models.Data;
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
}