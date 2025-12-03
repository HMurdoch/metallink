using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MetalLink.Application.Interfaces;
using MetalLink.Infrastructure.Persistence;
using MetalLink.Infrastructure.Persistence.Repositories;
using MetalLink.Infrastructure.Security;
using MetalLink.Infrastructure.Storage;

namespace MetalLink.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("MetalLinkDatabase");

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
        services.AddScoped<IOperatorRepository, OperatorRepository>();
        services.AddScoped<ICustomerDocumentRepository, CustomerDocumentRepository>();
        services.AddScoped<ITicketRepository, TicketRepository>();

        // Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Token service
        services.AddSingleton<ITokenService, TokenService>();

        // ✅ Password hasher
        services.AddSingleton<IPasswordHasher, PasswordHasher>();

        // File storage (S3 / MinIO)
        services.AddSingleton<IFileStorage, S3FileStorage>();

        return services;
    }
}