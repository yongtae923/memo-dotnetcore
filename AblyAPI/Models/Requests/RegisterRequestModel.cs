using System.ComponentModel.DataAnnotations;
using AblyAPI.Models.Data;

namespace AblyAPI.Models.Requests;

public class RegisterRequestModel
{
    [DataType(DataType.EmailAddress)]
    public string Email { get; set; }
    public string Password { get; set; }
    public string Name { get; set; }
    public string Nickname { get; set; }
    [DataType(DataType.PhoneNumber)]
    public string Phone { get; set; }

    public Account ToAccount(string parsedPhone) => new()
    {
        Id = Ulid.NewUlid().ToString(),
        Name = Name,
        Nickname = Nickname,
        Phone = parsedPhone,
        Email = Email,
        CreatedAt = DateTimeOffset.UtcNow,
        Credentials = new List<Credential> {new()
        {
            Provider = Providers.Self,
            LastUpdatedAt = DateTimeOffset.UtcNow,
            Password = Password
        }},
        AccessTokens = new List<AccessToken>()
    };
}