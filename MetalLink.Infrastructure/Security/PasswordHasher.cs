using System.Security.Cryptography;
using System.Text;
using MetalLink.Application.Interfaces;

namespace MetalLink.Infrastructure.Security;

public sealed class PasswordHasher : IPasswordHasher
{
    public string HashPassword(string password, string salt)
    {
        using var sha = SHA256.Create();
        var combined = Encoding.UTF8.GetBytes(password + "|" + salt);
        var hash = sha.ComputeHash(combined);
        return Convert.ToBase64String(hash);
    }

    public bool VerifyPassword(string password, string salt, string expectedHash)
    {
        var hash = HashPassword(password, salt);
        return hash == expectedHash;
    }
}
