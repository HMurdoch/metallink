using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MetalLink.Application.Interfaces;
using MetalLink.Domain.Entities;
using MetalLink.Shared.Buyers;

namespace MetalLink.Infrastructure.Persistence.Repositories;

public class BuyerRepository : IBuyerRepository
{
    private readonly MetalLinkDbContext _db;

    public BuyerRepository(MetalLinkDbContext db)
    {
        _db = db;
    }

    // -------------------------------------------------
    // Basic CRUD-style operations
    // -------------------------------------------------

    public Task<Buyer?> GetByIdAsync(long buyerId)
    {
        return GetByIdAsync(buyerId, CancellationToken.None);
    }

    public async Task<Buyer?> GetByIdAsync(
        long buyerId,
        CancellationToken cancellationToken)
    {
        return await _db.Buyers
            .Include(c => c.Company!)
            .Include(c => c.Site!)
                .ThenInclude(s => s!.Province!)
            .Include(c => c.Site!)
                .ThenInclude(s => s!.Country!)
            .Include(c => c.ImagePath!)
            .FirstOrDefaultAsync(c => c.BuyerId == buyerId, cancellationToken);
    }

    public async Task<Buyer?> GetByAccountNumberAsync(
        long accountNumber,
        CancellationToken cancellationToken = default)
    {
        return await _db.Buyers
            .Include(c => c.Company!)
            .Include(c => c.Site!)
                .ThenInclude(s => s!.Province!)
            .FirstOrDefaultAsync(
                c => c.AccountNumber == accountNumber,
                cancellationToken);
    }


    public async Task<long> GetNextAccountNumberAsync(CancellationToken ct)
    {
        // Account numbers must be globally unique across BOTH customers and buyers.
        // Use the shared generator function (creates it if missing) rather than a buyer-specific sequence.
        var generator = new AccountNumberGenerator(_db);
        return await generator.GetNextAsync(ct);
    }

    public async Task AddAsync(
        Buyer buyer,
        CancellationToken cancellationToken = default)
    {
        await _db.Buyers.AddAsync(buyer, cancellationToken);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsDuplicatePrimaryKey(ex))
        {
            // If the DB was restored/imported without syncing sequences, Postgres may try to
            // reuse an existing primary key value (23505 ..._pkey). Re-sync sequences and retry once.
            await PostgresSequenceSynchronizer.SyncAllIdentitySequencesAsync(_db, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    private static bool IsDuplicatePrimaryKey(DbUpdateException ex)
    {
        if (ex.InnerException is not Npgsql.PostgresException pg)
            return false;

        return pg.SqlState == "23505" &&
               pg.ConstraintName != null &&
               pg.ConstraintName.EndsWith("_pkey", StringComparison.OrdinalIgnoreCase);
    }

    // -------------------------------------------------
    // NEW: DTO-based search (used by SearchBuyersQueryHandler)
    // -------------------------------------------------

    public async Task<IReadOnlyList<Buyer>> SearchAsync(
        BuyerSearchRequestDto request,
        CancellationToken cancellationToken = default)
    {
        const int MaxResults = 500;

        var query = _db.Buyers
            .Include(c => c.Company!)
            .Include(c => c.Site!)
                .ThenInclude(s => s!.Province!)
            .Include(c => c.Site!)
                .ThenInclude(s => s!.Country!)
            .Include(c => c.ImagePath!)
            .Where(c => c.IsActive)
            .AsQueryable();

        // -------------------
        // Id / Site / Company
        // -------------------
        if (request.BuyerId.HasValue)
        {
            var id = request.BuyerId.Value;
            query = query.Where(c => c.BuyerId == id);
        }

        if (request.SiteId.HasValue)
        {
            var siteId = request.SiteId.Value;
            query = query.Where(c => c.SiteId == siteId);
        }

        if (!string.IsNullOrWhiteSpace(request.CompanyName))
        {
            var term = request.CompanyName.ToLower();
            query = query.Where(c =>
                c.Company != null &&
                c.Company.CompanyName != null &&
                c.Company.CompanyName.ToLower().Contains(term));
        }

        // -------------  Names
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

        // -------------  Other filters (same as you had)
        if (!string.IsNullOrWhiteSpace(request.IdNumber))
        {
            var term = request.IdNumber.ToLower();
            query = query.Where(c =>
                c.IdNumber != null &&
                c.IdNumber.ToLower().Contains(term));
        }

        if (request.AccountNumber.HasValue)
        {
            var acc = request.AccountNumber.Value;
            query = query.Where(c => c.AccountNumber == acc);
        }

        if (!string.IsNullOrWhiteSpace(request.PriceCode))
        {
            var term = request.PriceCode.ToLower();
            query = query.Where(c =>
                c.PriceCode != null &&
                c.PriceCode.ToLower().Contains(term));
        }

        // Province filter (site)
        if (request.ProvinceId.HasValue)
        {
            var provinceId = request.ProvinceId.Value;
            query = query.Where(c =>
                c.Site != null &&
                c.Site.ProvinceId == provinceId);
        }

        // Country filter (site)
        if (request.CountryId.HasValue)
        {
            var countryId = request.CountryId.Value;
            query = query.Where(c =>
                c.Site != null &&
                c.Site.CountryId == countryId);
        }

        // Taxable filter removed - return all records regardless of is_taxable status
        // The Taxable property in request is now ignored for search purposes

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

        return await query
            .OrderBy(c => c.BuyerId)
            .Take(MaxResults)
            .ToListAsync(cancellationToken);
    }


    // -------------------------------------------------
    // LEGACY: flattened-parameter search
    // (kept for compatibility, implemented via DTO)
    // -------------------------------------------------

    public async Task<IReadOnlyList<Buyer>> SearchAsync(
        int? buyerId,
        int? siteId,
        string? firstName,
        string? lastName,
        string? companyName,
        string? idNumber,
        long? accountNumber,
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
        // implementation relied on a Buyer.FullName property that
        // EF Core could not translate. Callers should prefer passing
        // FirstName / LastName via the DTO-based overload instead.

        var criteria = new BuyerSearchRequestDto
        {
            BuyerId    = buyerId,
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

    public async Task UpdateAsync(Buyer buyer, CancellationToken cancellationToken = default)
    {
        _db.Buyers.Update(buyer);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task SoftDeleteAsync(int buyerId, CancellationToken cancellationToken = default)
    {
        var buyer = await _db.Buyers
            .FirstOrDefaultAsync(c => c.BuyerId == buyerId, cancellationToken);

        if (buyer == null)
            return;

        buyer.IsActive = false;
        buyer.UpdatedTime = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }
}
