using MetalLink.Domain.Entities;

namespace MetalLink.Application.Interfaces;

public interface ITokenService
{
    string GenerateToken(Operator op);
}
