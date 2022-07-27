using System.Text;

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
        set => _passwordKey = Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
    }

    public bool VerifyPassword(string password)
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(
            Convert.ToBase64String(Encoding.UTF8.GetBytes(password)))) == _passwordKey;
    }

    public Credential() { }

    public Credential(Account account, string password)
    {
        Provider = Providers.Self;
        LastUpdatedAt = DateTimeOffset.UtcNow;
        AccountId = account.Id;
        Password = password;
    }
}

public enum Providers
{
    Self
}