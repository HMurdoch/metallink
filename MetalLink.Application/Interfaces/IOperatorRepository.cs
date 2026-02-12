using MetalLink.Domain.Entities;

namespace MetalLink.Application.Interfaces;

public interface IOperatorRepository
{
    Task<Operator?> GetByIdAsync(int operatorId, CancellationToken cancellationToken = default);
    Task<Operator?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task AddAsync(Operator op, CancellationToken cancellationToken = default);
}
