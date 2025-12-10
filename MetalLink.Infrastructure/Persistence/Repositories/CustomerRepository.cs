using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
            .Include(c => c.Company)
            .Include(c => c.Site)
                .ThenInclude(s => s.Province)
            .FirstOrDefaultAsync(c => c.CustomerId == customerId, cancellationToken);
    }

    public Task<Customer?> GetByAccountNumberAsync(
        string accountNumber,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Customers
            .Include(c => c.Company)
            .Include(c => c.Site)
                .ThenInclude(s => s.Province)
            .FirstOrDefaultAsync(c => c.AccountNumber == accountNumber, cancellationToken);
    }

    public Task AddAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        return _dbContext.Customers.AddAsync(customer, cancellationToken).AsTask();
    }

    /// <summary>
    /// Primary DTO-based search. This is the one called from the Desktop app via MediatR.
    /// It simply maps into the parameter-based overload so that all filter logic lives in one place.
    /// </summary>
    public Task<IReadOnlyList<Customer>> SearchAsync(
        CustomerSearchRequestDto criteria,
        CancellationToken cancellationToken = default)
    {
        // Merge first + last name into a single "full name" term (still stored in Customer.FullName)
        string? fullNameFilter = null;
        if (!string.IsNullOrWhiteSpace(criteria.FirstName) ||
            !string.IsNullOrWhiteSpace(criteria.LastName))
        {
            fullNameFilter = $"{criteria.FirstName} {criteria.LastName}".Trim();
        }

        return SearchAsync(
            customerId:    criteria.CustomerId,
            siteId:        criteria.SiteId,
            fullName:      fullNameFilter,
            companyName:   criteria.CompanyName,
            idNumber:      criteria.IdNumber,
            accountNumber: criteria.AccountNumber,
            priceCode:     criteria.PriceCode,
            addressLine1:  criteria.AddressLine1,
            addressLine2:  criteria.AddressLine2,
            suburb:        criteria.Suburb,
            city:          criteria.City,
            postalCode:    criteria.PostalCode,
            phoneNumber:   criteria.PhoneNumber,
            mobileNumber:  criteria.MobileNumber,
            email:         criteria.Email,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Core search implementation. All repository search paths end up here.
    /// Note: address / suburb / city / postal code now live on Site; company name lives on Company.
    /// </summary>
    public async Task<IReadOnlyList<Customer>> SearchAsync(
        long? customerId,
        long? siteId,
        string? fullName,
        string? companyName,
        string? idNumber,
        string? accountNumber,
        string? priceCode,
        string? addressLine1,
        string? addressLine2,
        string? suburb,
        string? city,
        string? postalCode,
        string? phoneNumber,
        string? mobileNumber,
        string? email,
        CancellationToken cancellationToken = default)
    {
        // Always include navigation properties so the API / DTO mapping has everything it needs.
        IQueryable<Customer> query = _dbContext.Customers
            .Include(c => c.Company)
            .Include(c => c.Site)
                .ThenInclude(s => s.Province);

        // --- ID filters ---

        if (customerId.HasValue)
            query = query.Where(c => c.CustomerId == customerId.Value);

        if (siteId.HasValue)
            query = query.Where(c => c.SiteId == siteId.Value);

        // --- Name / company filters ---

        if (!string.IsNullOrWhiteSpace(fullName))
        {
            var term = fullName.Trim().ToLower();
            query = query.Where(c =>
                c.FullName != null &&
                c.FullName.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(companyName))
        {
            var term = companyName.Trim().ToLower();
            query = query.Where(c =>
                c.Company != null &&
                c.Company.CompanyName != null &&
                c.Company.CompanyName.ToLower().Contains(term));
        }

        // --- Identity / account filters ---

        if (!string.IsNullOrWhiteSpace(idNumber))
        {
            var term = idNumber.Trim().ToLower();
            query = query.Where(c =>
                c.IdNumber != null &&
                c.IdNumber.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(accountNumber))
        {
            var term = accountNumber.Trim().ToLower();
            query = query.Where(c =>
                c.AccountNumber != null &&
                c.AccountNumber.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(priceCode))
        {
            var term = priceCode.Trim().ToLower();
            query = query.Where(c =>
                c.PriceCode != null &&
                c.PriceCode.ToLower().Contains(term));
        }

        // --- Address filters (now on Site) ---

        if (!string.IsNullOrWhiteSpace(addressLine1))
        {
            var term = addressLine1.Trim().ToLower();
            query = query.Where(c =>
                c.Site != null &&
                c.Site.AddressLine1 != null &&
                c.Site.AddressLine1.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(addressLine2))
        {
            var term = addressLine2.Trim().ToLower();
            query = query.Where(c =>
                c.Site != null &&
                c.Site.AddressLine2 != null &&
                c.Site.AddressLine2.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(suburb))
        {
            var term = suburb.Trim().ToLower();
            query = query.Where(c =>
                c.Site != null &&
                c.Site.Suburb != null &&
                c.Site.Suburb.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(city))
        {
            var term = city.Trim().ToLower();
            query = query.Where(c =>
                c.Site != null &&
                c.Site.City != null &&
                c.Site.City.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(postalCode))
        {
            var term = postalCode.Trim().ToLower();
            query = query.Where(c =>
                c.Site != null &&
                c.Site.PostalCode != null &&
                c.Site.PostalCode.ToLower().Contains(term));
        }

        // --- Contact filters ---

        if (!string.IsNullOrWhiteSpace(phoneNumber))
        {
            var term = phoneNumber.Trim().ToLower();
            query = query.Where(c =>
                c.PhoneNumber != null &&
                c.PhoneNumber.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(mobileNumber))
        {
            var term = mobileNumber.Trim().ToLower();
            query = query.Where(c =>
                c.MobileNumber != null &&
                c.MobileNumber.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(email))
        {
            var term = email.Trim().ToLower();
            query = query.Where(c =>
                c.Email != null &&
                c.Email.ToLower().Contains(term));
        }

        // Safety: cap results so a wildcard search doesn't nuke the DB
        return await query
            .OrderBy(c => c.CustomerId)
            .Take(200)
            .ToListAsync(cancellationToken);
    }
}
