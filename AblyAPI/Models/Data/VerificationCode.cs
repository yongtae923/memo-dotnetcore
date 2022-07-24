using System.ComponentModel.DataAnnotations;

namespace AblyAPI.Models.Data;

public class VerificationCode
{
    public string Id { get; set; }
    [DataType(DataType.PhoneNumber)]
    public string Phone { get; set; }
    public string Code { get; set; }
    public DateTimeOffset? VerifiesAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }

    public VerificationCode(string phone)
    {
        Id = Ulid.NewUlid().ToString();
        Phone = phone;
        Code = new Random().Next(100000, 1000000).ToString();
        VerifiesAt = null;
        ExpiresAt = DateTimeOffset.Now.AddMinutes(5);
    }
}