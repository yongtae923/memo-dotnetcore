namespace AblyAPI.Models.Data;

public class Credential
{
    public Providers Provider { get; set; }
    public DateTimeOffset LastUpdatedAt { get; set; }
    
    public string AccountId { get; set; }
    public Account Account { get; set; }
    
    private string _passwordKey;

    public string Password
    {
        get => _passwordKey;
        set => _passwordKey = BCrypt.Net.BCrypt.HashPassword(value);
    }

    public bool VerifyPassword(string password)
    {
        return BCrypt.Net.BCrypt.Verify(password, _passwordKey);
    }
}

public enum Providers
{
    Self
}