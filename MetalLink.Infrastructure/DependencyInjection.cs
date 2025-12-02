using Amazon.S3;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MetalLink.Application.Storage;
using MetalLink.Infrastructure.Persistence;
using MetalLink.Infrastructure.Storage;

namespace MetalLink.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<MetalLinkDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsql =>
            {
                // Put EF migrations history table inside the metal_link schema
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "metal_link");
            });
        });

        // S3 / MinIO wiring (leave as-is)
        var s3Section = configuration.GetSection("S3");
        var serviceUrl = s3Section["ServiceURL"];
        var accessKey = s3Section["AccessKey"];
        var secretKey = s3Section["SecretKey"];
        var forcePathStyle = bool.TryParse(s3Section["ForcePathStyle"], out var fps) && fps;

        services.AddSingleton<IAmazonS3>(_ =>
            new AmazonS3Client(
                accessKey,
                secretKey,
                new AmazonS3Config
                {
                    ServiceURL = serviceUrl,
                    ForcePathStyle = forcePathStyle,
                    AuthenticationRegion = "us-east-1"
                }));

        services.AddScoped<IObjectStorage, S3ObjectStorage>();

        return services;
    }
}
