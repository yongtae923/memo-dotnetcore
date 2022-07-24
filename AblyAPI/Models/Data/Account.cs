namespace AblyAPI.Models.Data;

public class Account
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Nickname { get; set; }
    public string Phone { get; set; }
    public string Email { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    
    public List<Credential> Credentials { get; set; }
    public List<AccessToken> AccessTokens { get; set; }
}