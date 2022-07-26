namespace AblyAPI.Models.Data;

public class AccessToken
{
    public string Token { get; set; }
    public string RefreshToken { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    
    public string AccountId { get; set; }
    public Account Account { get; set; }

    public AccessToken(Account account)
    {
        Token = Ulid.NewUlid().ToString();
        RefreshToken = Ulid.NewUlid().ToString();
        ExpiresAt = DateTimeOffset.UtcNow.AddDays(7);
        Account = account;
        AccountId = account.Id;
    }
}