using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using MetalLink.Application.Interfaces;
using MetalLink.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
namespace MetalLink.Infrastructure.Security;

public sealed class TokenService : ITokenService
{
    private readonly string _issuer;
    private readonly string _audience;
    private readonly byte[] _keyBytes;

    public TokenService(IConfiguration configuration)
    {
        var section = configuration.GetSection("JwtSettings");
        var key = section["SigningKey"] ?? throw new InvalidOperationException("JwtSettings:SigningKey not configured");

        _issuer = section["Issuer"] ?? "MetalLink";
        _audience = section["Audience"] ?? "MetalLinkClients";
        _keyBytes = Encoding.UTF8.GetBytes(key);
    }

    public string GenerateToken(Operator op)
    {
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, op.OperatorId.ToString()),
            new Claim("username", op.Username),
            new Claim("display_name", op.DisplayName),
            new Claim(ClaimTypes.Role, op.Role)
        };

        var signingKey = new SymmetricSecurityKey(_keyBytes);
        var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
