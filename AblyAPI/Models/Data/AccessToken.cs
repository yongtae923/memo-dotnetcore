namespace AblyAPI.Models.Data;

public class AccessToken
{
    public string Token { get; set; }
    public string RefreshToken { get; set; }
    public string PriviousToken { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    
    public string AccountId { get; set; }
    public Account Account { get; set; }
}