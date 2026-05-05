using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MetalLink.Application.Interfaces;
using MetalLink.Application.Services;
using MetalLink.Infrastructure.Persistence;
using MetalLink.Infrastructure.Persistence.Repositories;
using MetalLink.Infrastructure.Security;
using MetalLink.Infrastructure.Storage;
using Npgsql;

namespace MetalLink.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("MetalLinkDatabase");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            var databaseUrl = configuration["DATABASE_URL"];
            if (!string.IsNullOrWhiteSpace(databaseUrl))
            {
                connectionString = ConvertDatabaseUrlToConnectionString(databaseUrl);
            }
        }

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Database connection string is not configured.");
        }

        services.AddDbContext<MetalLinkDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });

        // File storage options (your existing mapping)
        var fileStorageSection = configuration.GetSection("FileStorage");
        services.Configure<FileStorageOptions>(options =>
        {
            options.BucketName = fileStorageSection["BucketName"] ?? string.Empty;
            options.ServiceUrl = fileStorageSection["ServiceUrl"];
            options.UseMinio = bool.TryParse(fileStorageSection["UseMinio"], out var useMinio) && useMinio;
            options.Region = fileStorageSection["Region"] ?? "af-south-1";
        });

        // Repositories
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IBuyerRepository, BuyerRepository>();
        services.AddScoped<IOperatorRepository, OperatorRepository>();
        services.AddScoped<ICompanyRepository, CompanyRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IPriceRepository, PriceRepository>();
        services.AddScoped<IProductPriceListRepository, ProductPriceListRepository>();
        services.AddScoped<IProductPriceListProductPriceRepository, ProductPriceListProductPriceRepository>();
        services.AddScoped<ITicketReceivingRepository, TicketReceivingRepository>();
        services.AddScoped<ITicketSendingRepository, TicketSendingRepository>();
        services.AddScoped<IStockLevelRepository, StockLevelRepository>();
        services.AddScoped<IStockMovementRepository, StockMovementRepository>();

        // Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Application Services
        services.AddScoped<TicketNumberService>();
        services.AddScoped<IAccountNumberGenerator, AccountNumberGenerator>();

        // Token service
        services.AddSingleton<ITokenService, TokenService>();

        // ✅ Password hasher
        services.AddSingleton<IPasswordHasher, PasswordHasher>();

        // File storage (S3 / MinIO)
        services.AddSingleton<IFileStorage, S3FileStorage>();

        return services;
    }

    private static string ConvertDatabaseUrlToConnectionString(string databaseUrl)
    {
        var uri = new Uri(databaseUrl);
        var userInfo = uri.UserInfo.Split(':', 2);

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Username = Uri.UnescapeDataString(userInfo[0]),
            Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty,
            Database = uri.AbsolutePath.TrimStart('/'),
            SslMode = SslMode.Require,
            TrustServerCertificate = true
        };

        return builder.ToString();
    }
}