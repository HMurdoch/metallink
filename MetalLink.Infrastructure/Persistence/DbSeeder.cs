using MetalLink.Domain.Entities;
using MetalLink.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MetalLink.Infrastructure.Persistence;

public static class DbSeeder
{
    public static async Task SeedAsync(
        MetalLinkDbContext dbContext,
        IPasswordHasher passwordHasher)
    {
        await dbContext.Database.MigrateAsync();

        // Seed Countries
        await SeedCountriesAsync(dbContext);
        
        // Seed Provinces
        await SeedProvincesAsync(dbContext);
        
        // Seed Companies
        await SeedCompaniesAsync(dbContext);
        
        // Seed Sites
        await SeedSitesAsync(dbContext);
        
        // Seed Currencies
        await SeedCurrenciesAsync(dbContext);
        
        // Seed Products
        await SeedProductsAsync(dbContext);
        
        // Seed Prices
        await SeedPricesAsync(dbContext);
        
        // Seed Buyers
        await SeedBuyersAsync(dbContext);
        
        // Seed Operators
        await SeedOperatorsAsync(dbContext, passwordHasher);
    }

    private static async Task SeedCountriesAsync(MetalLinkDbContext dbContext)
    {
        if (await dbContext.Countries.AnyAsync())
            return;

        var now = DateTime.UtcNow;
        var countries = new[]
        {
            new Country { Name = "South Africa", Code = "ZA", IsActive = true, CreatedTime = now, UpdatedTime = now },
            new Country { Name = "Namibia", Code = "NA", IsActive = true, CreatedTime = now, UpdatedTime = now },
            new Country { Name = "Botswana", Code = "BW", IsActive = true, CreatedTime = now, UpdatedTime = now },
            new Country { Name = "Zimbabwe", Code = "ZW", IsActive = true, CreatedTime = now, UpdatedTime = now },
            new Country { Name = "Mozambique", Code = "MZ", IsActive = true, CreatedTime = now, UpdatedTime = now }
        };

        await dbContext.Countries.AddRangeAsync(countries);
        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedProvincesAsync(MetalLinkDbContext dbContext)
    {
        if (await dbContext.Provinces.AnyAsync())
            return;

        var now = DateTime.UtcNow;
        var provinces = new[]
        {
            new Province { ProvinceName = "Eastern Cape", ProvinceCode = "EC", IsActive = true, CreatedTime = now, UpdatedTime = now },
            new Province { ProvinceName = "Free State", ProvinceCode = "FS", IsActive = true, CreatedTime = now, UpdatedTime = now },
            new Province { ProvinceName = "Gauteng", ProvinceCode = "GT", IsActive = true, CreatedTime = now, UpdatedTime = now },
            new Province { ProvinceName = "KwaZulu-Natal", ProvinceCode = "KZN", IsActive = true, CreatedTime = now, UpdatedTime = now },
            new Province { ProvinceName = "Limpopo", ProvinceCode = "LP", IsActive = true, CreatedTime = now, UpdatedTime = now },
            new Province { ProvinceName = "Mpumalanga", ProvinceCode = "MP", IsActive = true, CreatedTime = now, UpdatedTime = now },
            new Province { ProvinceName = "Northern Cape", ProvinceCode = "NC", IsActive = true, CreatedTime = now, UpdatedTime = now },
            new Province { ProvinceName = "North West", ProvinceCode = "NW", IsActive = true, CreatedTime = now, UpdatedTime = now },
            new Province { ProvinceName = "Western Cape", ProvinceCode = "WC", IsActive = true, CreatedTime = now, UpdatedTime = now }
        };

        await dbContext.Provinces.AddRangeAsync(provinces);
        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedCompaniesAsync(MetalLinkDbContext dbContext)
    {
        if (await dbContext.Companies.AnyAsync())
            return;

        var now = DateTimeOffset.UtcNow;
        var companies = new[]
        {
            new Company 
            { 
                CompanyName = "MetalLink Recycling (Pty) Ltd", 
                VatNumber = "4123456789", 
                IsActive = true, 
                CreatedTime = now, 
                UpdatedTime = now 
            },
            new Company 
            { 
                CompanyName = "ScrapMetal Solutions", 
                VatNumber = "4987654321", 
                IsActive = true, 
                CreatedTime = now, 
                UpdatedTime = now 
            }
        };

        await dbContext.Companies.AddRangeAsync(companies);
        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedSitesAsync(MetalLinkDbContext dbContext)
    {
        if (await dbContext.Sites.AnyAsync())
            return;

        var company = await dbContext.Companies.FirstAsync();
        var province = await dbContext.Provinces.FirstOrDefaultAsync(p => p.ProvinceCode == "GT");
        var country = await dbContext.Countries.FirstOrDefaultAsync(c => c.Code == "ZA");
        
        var now = DateTimeOffset.UtcNow;
        var sites = new[]
        {
            new Site
            {
                CompanyId = company.CompanyId,
                SiteName = "Johannesburg Depot",
                SiteCode = "JHB001",
                AddressLine1 = "123 Industrial Road",
                Suburb = "Booysens",
                City = "Johannesburg",
                PostalCode = "2091",
                ProvinceId = province?.ProvinceId,
                CountryId = country?.CountryId,
                IsActive = true,
                CreatedTime = now,
                UpdatedTime = now
            },
            new Site
            {
                CompanyId = company.CompanyId,
                SiteName = "Pretoria Depot",
                SiteCode = "PTA001",
                AddressLine1 = "456 Scrap Avenue",
                Suburb = "Rosslyn",
                City = "Pretoria",
                PostalCode = "0200",
                ProvinceId = province?.ProvinceId,
                CountryId = country?.CountryId,
                IsActive = true,
                CreatedTime = now,
                UpdatedTime = now
            }
        };

        await dbContext.Sites.AddRangeAsync(sites);
        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedCurrenciesAsync(MetalLinkDbContext dbContext)
    {
        if (await dbContext.Currencies.AnyAsync())
            return;

        var now = DateTimeOffset.UtcNow;
        var currencies = new[]
        {
            new Currency { CurrencyCode = "ZAR", CurrencyDescription = "South African Rand", IsActive = true, CreatedTime = now, UpdatedTime = now },
            new Currency { CurrencyCode = "USD", CurrencyDescription = "United States Dollar", IsActive = true, CreatedTime = now, UpdatedTime = now },
            new Currency { CurrencyCode = "EUR", CurrencyDescription = "Euro", IsActive = true, CreatedTime = now, UpdatedTime = now }
        };

        await dbContext.Currencies.AddRangeAsync(currencies);
        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedProductsAsync(MetalLinkDbContext dbContext)
    {
        if (await dbContext.Products.AnyAsync())
            return;

        var now = DateTimeOffset.UtcNow;
        var products = new[]
        {
            new Product { ProductCode = "COPPER", ProductName = "Copper Wire", Grade = 99.9m, IsActive = true, CreatedTime = now, UpdatedTime = now },
            new Product { ProductCode = "BRASS", ProductName = "Brass Fittings", Grade = 85.0m, IsActive = true, CreatedTime = now, UpdatedTime = now },
            new Product { ProductCode = "STEEL", ProductName = "Steel Scrap", Grade = 95.0m, IsActive = true, CreatedTime = now, UpdatedTime = now },
            new Product { ProductCode = "ALUMINUM", ProductName = "Aluminum Cans", Grade = 92.0m, IsActive = true, CreatedTime = now, UpdatedTime = now },
            new Product { ProductCode = "STAINLESS", ProductName = "Stainless Steel", Grade = 98.0m, IsActive = true, CreatedTime = now, UpdatedTime = now },
            new Product { ProductCode = "LEAD", ProductName = "Lead Batteries", Grade = 90.0m, IsActive = true, CreatedTime = now, UpdatedTime = now },
            new Product { ProductCode = "ZINC", ProductName = "Zinc Scrap", Grade = 88.0m, IsActive = true, CreatedTime = now, UpdatedTime = now },
            new Product { ProductCode = "IRON", ProductName = "Cast Iron", Grade = 93.0m, IsActive = true, CreatedTime = now, UpdatedTime = now }
        };

        await dbContext.Products.AddRangeAsync(products);
        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedPricesAsync(MetalLinkDbContext dbContext)
    {
        if (await dbContext.Prices.AnyAsync())
            return;

        var products = await dbContext.Products.ToListAsync();
        var now = DateTimeOffset.UtcNow;
        
        var prices = new List<Price>();
        foreach (var product in products)
        {
            decimal basePrice = product.ProductCode switch
            {
                "COPPER" => 120.00m,
                "BRASS" => 75.00m,
                "STEEL" => 8.50m,
                "ALUMINUM" => 18.00m,
                "STAINLESS" => 45.00m,
                "LEAD" => 15.00m,
                "ZINC" => 25.00m,
                "IRON" => 6.00m,
                _ => 10.00m
            };

            prices.Add(new Price
            {
                ProductId = product.ProductId,
                PriceA = basePrice,
                PriceB = basePrice * 0.95m,
                PriceC = basePrice * 0.90m,
                IsActive = true,
                CreatedTime = now,
                UpdatedTime = now
            });
        }

        await dbContext.Prices.AddRangeAsync(prices);
        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedBuyersAsync(MetalLinkDbContext dbContext)
    {
        if (await dbContext.Buyers.AnyAsync())
            return;

        var company = await dbContext.Companies.FirstAsync();
        var site = await dbContext.Sites.FirstAsync();
        var now = DateTimeOffset.UtcNow;

        var buyers = new[]
        {
            new Buyer
            {
                CompanyId = company.CompanyId,
                SiteId = site.SiteId,
                BuyerName = "Scrap Metal Exports (Pty) Ltd",
                ContactPerson = "John Smith",
                IsCompany = true,
                RegistrationNumber = "2010/123456/07",
                VatNumber = "4567891234",
                AccountNumber = 1001,
                PriceCode = "A",
                PhoneNumber = "+27 11 123 4567",
                Email = "john@scrapexports.co.za",
                Address = "45 Export Drive, Kempton Park, 1619",
                Taxable = true,
                PaymentTerms = "30 days",
                Notes = "Preferred buyer for copper and brass",
                IsActive = true,
                CreatedTime = now,
                UpdatedTime = now
            },
            new Buyer
            {
                CompanyId = company.CompanyId,
                SiteId = site.SiteId,
                BuyerName = "Global Metal Traders",
                ContactPerson = "Sarah Johnson",
                IsCompany = true,
                RegistrationNumber = "2015/987654/07",
                VatNumber = "4111222333",
                AccountNumber = 1002,
                PriceCode = "B",
                PhoneNumber = "+27 11 987 6543",
                Email = "sarah@globalmetals.com",
                Address = "78 Trade Street, Sandton, 2196",
                Taxable = true,
                PaymentTerms = "COD",
                Notes = "Large volume steel buyer",
                IsActive = true,
                CreatedTime = now,
                UpdatedTime = now
            },
            new Buyer
            {
                CompanyId = company.CompanyId,
                SiteId = site.SiteId,
                BuyerName = "Eastern Cape Recycling",
                ContactPerson = "Michael Brown",
                IsCompany = true,
                RegistrationNumber = "2018/456789/07",
                VatNumber = "4222333444",
                AccountNumber = 1003,
                PriceCode = "C",
                PhoneNumber = "+27 41 555 7890",
                Email = "michael@ecrecycling.co.za",
                Address = "12 Industrial Zone, Port Elizabeth, 6001",
                Taxable = true,
                PaymentTerms = "14 days",
                Notes = "Regional buyer for all metals",
                IsActive = true,
                CreatedTime = now,
                UpdatedTime = now
            }
        };

        await dbContext.Buyers.AddRangeAsync(buyers);
        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedOperatorsAsync(MetalLinkDbContext dbContext, IPasswordHasher passwordHasher)
    {
        if (await dbContext.Operators.AnyAsync())
            return;

        var site = await dbContext.Sites.FirstAsync();
        
        var username = "admin";
        var password = "Admin123!";
        var hash = passwordHasher.HashPassword(password, username);

        var op = new Operator(
            siteId: site.SiteId,
            username: username,
            displayName: "System Administrator",
            passwordHash: hash,
            role: "Admin"
        );

        await dbContext.Operators.AddAsync(op);
        await dbContext.SaveChangesAsync();
    }
}
