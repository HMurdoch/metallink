using Microsoft.EntityFrameworkCore;
using MetalLink.Application.Interfaces;
using MetalLink.Domain.Entities;

namespace MetalLink.Infrastructure.Persistence.Repositories;

public sealed class OperatorRepository : IOperatorRepository
{
    private readonly MetalLinkDbContext _dbContext;

    public OperatorRepository(MetalLinkDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Operator?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        return _dbContext.Operators
            .FirstOrDefaultAsync(o => o.Username == username && o.IsActive, cancellationToken);
    }

    public Task AddAsync(Operator op, CancellationToken cancellationToken = default)
    {
        return _dbContext.Operators.AddAsync(op, cancellationToken).AsTask();
    }
}
