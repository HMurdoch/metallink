namespace MetalLink.Desktop.Auth;

public sealed class AuthState
{
    public bool IsAuthenticated => !string.IsNullOrWhiteSpace(Token);

    public string Token { get; private set; } = string.Empty;
    public string Username { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public string Role { get; private set; } = string.Empty;
    public long SiteId { get; private set; }

    public void SetAuth(
        string token,
        string username,
        string displayName,
        string role,
        long siteId)
    {
        Token = token;
        Username = username;
        DisplayName = displayName;
        Role = role;
        SiteId = siteId;
    }

    public void Clear()
    {
        Token = string.Empty;
        Username = string.Empty;
        DisplayName = string.Empty;
        Role = string.Empty;
        SiteId = 0;
    }
}
