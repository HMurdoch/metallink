namespace MetalLink.Application.Interfaces;

public interface IPasswordHasher
{
    string HashPassword(string password, string salt);
    bool VerifyPassword(string password, string salt, string expectedHash);
}

