using Microsoft.EntityFrameworkCore;
using MetalLink.Application.Interfaces;
using MetalLink.Domain.Entities;

namespace MetalLink.Infrastructure.Persistence.Repositories;

public sealed class CustomerDocumentRepository : ICustomerDocumentRepository
{
    private readonly MetalLinkDbContext _dbContext;

    public CustomerDocumentRepository(MetalLinkDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(CustomerDocument document, CancellationToken cancellationToken = default)
    {
        await _dbContext.CustomerDocuments.AddAsync(document, cancellationToken);
    }

    public async Task<IReadOnlyList<CustomerDocument>> GetByCustomerIdAsync(long customerId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.CustomerDocuments
            .Where(d => d.CustomerId == customerId)
            .OrderByDescending(d => d.CreatedTime)
            .ToListAsync(cancellationToken);
    }
}
