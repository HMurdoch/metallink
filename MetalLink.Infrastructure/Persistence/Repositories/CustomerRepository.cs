using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MetalLink.Application.Interfaces;
using MetalLink.Domain.Entities;
using MetalLink.Shared.Customers;

namespace MetalLink.Infrastructure.Persistence.Repositories;

public class CustomerRepository : ICustomerRepository
{
    private readonly MetalLinkDbContext _db;

    public CustomerRepository(MetalLinkDbContext db)
    {
        _db = db;
    }

    // -------------------------------------------------
    // Basic CRUD-style operations
    // -------------------------------------------------

    public async Task<Customer?> GetByIdAsync(
        long customerId,
        CancellationToken cancellationToken = default)
    {
        return await _db.Customers
            .Include(c => c.Company)
            .Include(c => c.Site)
                .ThenInclude(s => s.Province)
            .FirstOrDefaultAsync(c => c.CustomerId == customerId, cancellationToken);
    }

    public async Task<Customer?> GetByAccountNumberAsync(
        string accountNumber,
        CancellationToken cancellationToken = default)
    {
        return await _db.Customers
            .Include(c => c.Company)
            .Include(c => c.Site)
                .ThenInclude(s => s.Province)
            .FirstOrDefaultAsync(
                c => c.AccountNumber != null && c.AccountNumber == accountNumber,
                cancellationToken);
    }

    public async Task AddAsync(
        Customer customer,
        CancellationToken cancellationToken = default)
    {
        await _db.Customers.AddAsync(customer, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    // -------------------------------------------------
    // NEW: DTO-based search (used by SearchCustomersQueryHandler)
    // -------------------------------------------------

    public async Task<IReadOnlyList<Customer>> SearchAsync(
        CustomerSearchRequestDto request,
        CancellationToken cancellationToken = default)
    {
        const int MaxResults = 500;

        var query = _db.Customers
            .Include(c => c.Company)
            .Include(c => c.Site)
                .ThenInclude(s => s.Province)
            .Where(c => c.IsActive)            // << only active customers
            .AsQueryable();

        // -------------------
        // Id / Site / Company
        // -------------------
        if (request.CustomerId.HasValue)
        {
            var id = request.CustomerId.Value;
            query = query.Where(c => c.CustomerId == id);
        }

        if (request.SiteId.HasValue)
        {
            var siteId = request.SiteId.Value;
            query = query.Where(c => c.SiteId == siteId);

            // When a specific Site is chosen, we don't also need
            // to filter by company name (site already implies company).
        }
        else if (!string.IsNullOrWhiteSpace(request.CompanyName))
        {
            var companyTerm = request.CompanyName.ToLower();
            query = query.Where(c =>
                c.Company != null &&
                c.Company.CompanyName != null &&
                c.Company.CompanyName.ToLower().Contains(companyTerm));
        }

        // -------------
        // Name filters
        // -------------
        if (!string.IsNullOrWhiteSpace(request.FirstName))
        {
            var term = request.FirstName.ToLower();
            query = query.Where(c =>
                c.FirstName != null &&
                c.FirstName.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(request.LastName))
        {
            var term = request.LastName.ToLower();
            query = query.Where(c =>
                c.LastName != null &&
                c.LastName.ToLower().Contains(term));
        }

        // -------------
        // Other filters
        // -------------
        if (!string.IsNullOrWhiteSpace(request.IdNumber))
        {
            var term = request.IdNumber.ToLower();
            query = query.Where(c =>
                c.IdNumber != null &&
                c.IdNumber.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(request.AccountNumber))
        {
            var term = request.AccountNumber.ToLower();
            query = query.Where(c =>
                c.AccountNumber != null &&
                c.AccountNumber.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(request.PriceCode))
        {
            var term = request.PriceCode.ToLower();
            query = query.Where(c =>
                c.PriceCode != null &&
                c.PriceCode.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(request.AddressLine1))
        {
            var term = request.AddressLine1.ToLower();
            query = query.Where(c =>
                c.Site != null &&
                c.Site.AddressLine1 != null &&
                c.Site.AddressLine1.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(request.AddressLine2))
        {
            var term = request.AddressLine2.ToLower();
            query = query.Where(c =>
                c.Site != null &&
                c.Site.AddressLine2 != null &&
                c.Site.AddressLine2.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(request.Suburb))
        {
            var term = request.Suburb.ToLower();
            query = query.Where(c =>
                c.Site != null &&
                c.Site.Suburb != null &&
                c.Site.Suburb.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(request.City))
        {
            var term = request.City.ToLower();
            query = query.Where(c =>
                c.Site != null &&
                c.Site.City != null &&
                c.Site.City.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(request.PostalCode))
        {
            var term = request.PostalCode.ToLower();
            query = query.Where(c =>
                c.Site != null &&
                c.Site.PostalCode != null &&
                c.Site.PostalCode.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            var term = request.PhoneNumber.ToLower();
            query = query.Where(c =>
                c.PhoneNumber != null &&
                c.PhoneNumber.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(request.MobileNumber))
        {
            var term = request.MobileNumber.ToLower();
            query = query.Where(c =>
                c.MobileNumber != null &&
                c.MobileNumber.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var term = request.Email.ToLower();
            query = query.Where(c =>
                c.Email != null &&
                c.Email.ToLower().Contains(term));
        }

        // Final shape
        query = query
            .OrderBy(c => c.CustomerId)
            .Take(MaxResults);

        return await query.ToListAsync(cancellationToken);
    }

    // -------------------------------------------------
    // LEGACY: flattened-parameter search
    // (kept for compatibility, implemented via DTO)
    // -------------------------------------------------

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
        // NOTE: we intentionally ignore fullName here, because the old
        // implementation relied on a Customer.FullName property that
        // EF Core could not translate. Callers should prefer passing
        // FirstName / LastName via the DTO-based overload instead.

        var criteria = new CustomerSearchRequestDto
        {
            CustomerId    = customerId,
            SiteId        = siteId,
            CompanyName   = companyName,
            IdNumber      = idNumber,
            AccountNumber = accountNumber,
            PriceCode     = priceCode,
            AddressLine1  = addressLine1,
            AddressLine2  = addressLine2,
            Suburb        = suburb,
            City          = city,
            PostalCode    = postalCode,
            PhoneNumber   = phoneNumber,
            MobileNumber  = mobileNumber,
            Email         = email,
            // FirstName / LastName intentionally left null here
        };

        return await SearchAsync(criteria, cancellationToken);
    }

    public async Task UpdateAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        _db.Customers.Update(customer);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task SoftDeleteAsync(long customerId, CancellationToken cancellationToken = default)
    {
        var customer = await _db.Customers
            .FirstOrDefaultAsync(c => c.CustomerId == customerId, cancellationToken);

        if (customer == null)
            return;

        customer.IsActive = false;
        await _db.SaveChangesAsync(cancellationToken);
    }
}
