using System.Linq;
using Microsoft.EntityFrameworkCore;
using MetalLink.Application.Interfaces;
using MetalLink.Domain.Entities;
using MetalLink.Shared.Customers;

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

    public async Task<IReadOnlyList<Customer>> SearchAsync(
        CustomerSearchRequestDto criteria,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Customers.AsQueryable();

        if (criteria.CustomerId.HasValue)
            query = query.Where(c => c.CustomerId == criteria.CustomerId.Value);

        if (criteria.SiteId.HasValue)
            query = query.Where(c => c.SiteId == criteria.SiteId.Value);

        // Treat first/last as wildcard filters on FullName
        if (!string.IsNullOrWhiteSpace(criteria.FirstName))
        {
            var first = criteria.FirstName.Trim().ToLower();
            query = query.Where(c => c.FullName.ToLower().Contains(first));
        }

        if (!string.IsNullOrWhiteSpace(criteria.LastName))
        {
            var last = criteria.LastName.Trim().ToLower();
            query = query.Where(c => c.FullName.ToLower().Contains(last));
        }

        if (!string.IsNullOrWhiteSpace(criteria.CompanyName))
        {
            var v = criteria.CompanyName.Trim().ToLower();
            query = query.Where(c => c.CompanyName != null && c.CompanyName.ToLower().Contains(v));
        }

        if (!string.IsNullOrWhiteSpace(criteria.IdNumber))
        {
            var v = criteria.IdNumber.Trim().ToLower();
            query = query.Where(c => c.IdNumber != null && c.IdNumber.ToLower().Contains(v));
        }

        if (!string.IsNullOrWhiteSpace(criteria.AccountNumber))
        {
            var v = criteria.AccountNumber.Trim().ToLower();
            query = query.Where(c => c.AccountNumber != null && c.AccountNumber.ToLower().Contains(v));
        }

        if (!string.IsNullOrWhiteSpace(criteria.PriceCode))
        {
            var v = criteria.PriceCode.Trim().ToLower();
            query = query.Where(c => c.PriceCode != null && c.PriceCode.ToLower().Contains(v));
        }

        if (!string.IsNullOrWhiteSpace(criteria.AddressLine1))
        {
            var v = criteria.AddressLine1.Trim().ToLower();
            query = query.Where(c => c.AddressLine1 != null && c.AddressLine1.ToLower().Contains(v));
        }

        if (!string.IsNullOrWhiteSpace(criteria.AddressLine2))
        {
            var v = criteria.AddressLine2.Trim().ToLower();
            query = query.Where(c => c.AddressLine2 != null && c.AddressLine2.ToLower().Contains(v));
        }

        if (!string.IsNullOrWhiteSpace(criteria.Suburb))
        {
            var v = criteria.Suburb.Trim().ToLower();
            query = query.Where(c => c.Suburb != null && c.Suburb.ToLower().Contains(v));
        }

        if (!string.IsNullOrWhiteSpace(criteria.City))
        {
            var v = criteria.City.Trim().ToLower();
            query = query.Where(c => c.City != null && c.City.ToLower().Contains(v));
        }

        if (!string.IsNullOrWhiteSpace(criteria.PostalCode))
        {
            var v = criteria.PostalCode.Trim().ToLower();
            query = query.Where(c => c.PostalCode != null && c.PostalCode.ToLower().Contains(v));
        }

        if (!string.IsNullOrWhiteSpace(criteria.PhoneNumber))
        {
            var v = criteria.PhoneNumber.Trim().ToLower();
            query = query.Where(c => c.PhoneNumber != null && c.PhoneNumber.ToLower().Contains(v));
        }

        if (!string.IsNullOrWhiteSpace(criteria.MobileNumber))
        {
            var v = criteria.MobileNumber.Trim().ToLower();
            query = query.Where(c => c.MobileNumber != null && c.MobileNumber.ToLower().Contains(v));
        }

        if (!string.IsNullOrWhiteSpace(criteria.Email))
        {
            var v = criteria.Email.Trim().ToLower();
            query = query.Where(c => c.Email != null && c.Email.ToLower().Contains(v));
        }

        return await query
            .OrderBy(c => c.CustomerId)
            .ToListAsync(cancellationToken);
    }
}
