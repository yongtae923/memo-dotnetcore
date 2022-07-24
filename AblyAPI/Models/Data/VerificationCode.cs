namespace AblyAPI.Models.Data;

public class VerificationCode
{
    public string Id { get; set; }
    public string Phone { get; set; }
    public string Code { get; set; }
    public DateTimeOffset VerifiesAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
}