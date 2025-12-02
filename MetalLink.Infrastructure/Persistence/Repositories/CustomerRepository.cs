using Microsoft.EntityFrameworkCore;
using MetalLink.Application.Interfaces;
using MetalLink.Domain.Entities;

namespace MetalLink.Infrastructure.Persistence.Repositories;

public sealed class CustomerRepository : ICustomerRepository
{
    private readonly MetalLinkDbContext _dbContext;

    public CustomerRepository(MetalLinkDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Customer?> GetByIdAsync(long customerId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Customers
            .FirstOrDefaultAsync(c => c.CustomerId == customerId, cancellationToken);
    }

    public Task<Customer?> GetByAccountNumberAsync(string accountNumber, CancellationToken cancellationToken = default)
    {
        return _dbContext.Customers
            .FirstOrDefaultAsync(c => c.AccountNumber == accountNumber, cancellationToken);
    }

    public Task AddAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        return _dbContext.Customers.AddAsync(customer, cancellationToken).AsTask();
    }
}
