using System;
using System.Collections.Generic;
using AblyAPI.Models.Data;
using AblyAPI.Models.Requests;
using PhoneNumbers;

namespace AblyAPITest.Helpers;

public class ServiceTestHelper
{
    private readonly PhoneNumberUtil _phone;

    protected ServiceTestHelper()
    {
        _phone = PhoneNumberUtil.GetInstance();
    }

    protected string ParseToFormat(string phone) => _phone.Format(_phone.Parse(phone, "KR"), PhoneNumberFormat.E164);

    protected static PhoneNumberRequestModel TestPhoneNumberRequestModel => new() {Phone = "01012345678"};

    protected static RegisterRequestModel TestRegisterRequestModel => new()
    {
        Email = "yongtae@a-bly.com",
        Password = "yongtae@ably!",
        Name = "Yongtae Kim",
        Nickname = "Ably-dev",
        Phone = "01012345678"
    };

    protected static LoginRequestModel TestLoginRequestModel => new()
    {
        Id = "yongtae@a-bly.com",
        Password = "yongtae@ably!"
    };
    
    protected static Account TestAccount() => new()
    {
        Id = Ulid.NewUlid().ToString(),
        Name = Ulid.NewUlid().ToString(),
        Nickname = Ulid.NewUlid().ToString(),
        Phone = "01012345678",
        Email = "yongtae@a-bly.com",
        CreatedAt = DateTimeOffset.UtcNow
    };

    protected Account TestAccount(RegisterRequestModel model) => new()
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
}
