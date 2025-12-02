using MetalLink.Application.Interfaces;

namespace MetalLink.Infrastructure.Persistence;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly MetalLinkDbContext _dbContext;

    public UnitOfWork(MetalLinkDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
