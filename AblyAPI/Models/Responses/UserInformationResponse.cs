using System.ComponentModel.DataAnnotations;
using AblyAPI.Models.Data;

namespace AblyAPI.Models.Responses;

public class UserInformationResponse
{
    public string Name { get; set; }
    public string Nickname { get; set; }
    [DataType(DataType.PhoneNumber)] public string Phone { get; set; }
    [DataType(DataType.EmailAddress)] public string Email { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public UserInformationResponse() { }

    public UserInformationResponse(Account account)
    {
        Name = account.Name;
        Nickname = account.Nickname;
        Phone = account.Phone;
        Email = account.Email;
        CreatedAt = account.CreatedAt;
    }
}