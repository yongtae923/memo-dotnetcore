using System.ComponentModel.DataAnnotations;

namespace AblyAPI.Models.Data;

public class Account
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Nickname { get; set; }
    [DataType(DataType.PhoneNumber)]
    public string Phone { get; set; }
    [DataType(DataType.EmailAddress)]
    public string Email { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    
    public List<Credential> Credentials { get; set; }
    public List<AccessToken> AccessTokens { get; set; }
}