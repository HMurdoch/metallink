namespace MetalLink.Shared.Auth;

public sealed class LoginRequestDto
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public sealed class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;
    public int OperatorId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int SiteId { get; set; }
    public System.Collections.Generic.List<MetalLink.Shared.Settings.OperatorSettingDto> OperatorSettings { get; set; } = new();
}
