using AblyAPI.Models.Data;

namespace AblyAPI.Models.Responses;

public class AccessTokenResponse
{
    public string AccountId { get; set; }
    public string Token { get; set; }
    public string RefreshToken { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    
    public AccessTokenResponse() { }

    public AccessTokenResponse(AccessToken accessToken)
    {
        AccountId = accessToken.AccountId;
        Token = accessToken.Token;
        RefreshToken = accessToken.RefreshToken;
        ExpiresAt = accessToken.ExpiresAt;
    }
}