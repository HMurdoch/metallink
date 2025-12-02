using MetalLink.Domain.Entities;

namespace MetalLink.Application.Interfaces;

public interface IOperatorRepository
{
    Task<Operator?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task AddAsync(Operator op, CancellationToken cancellationToken = default);
}
