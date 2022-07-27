using System.ComponentModel.DataAnnotations;
using AblyAPI.Models.Data;

namespace AblyAPI.Models.Requests;

public class RegisterRequestModel
{
    [Required]
    [DataType(DataType.EmailAddress)]
    public string Email { get; set; }
    [Required]
    public string Password { get; set; }
    [Required]
    public string Name { get; set; }
    [Required]
    public string Nickname { get; set; }
    [Required]
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
        Credentials = new List<Credential>(),
        AccessTokens = new List<AccessToken>()
    };
}