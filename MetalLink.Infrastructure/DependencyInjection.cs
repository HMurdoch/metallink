using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MetalLink.Application.Interfaces;
using MetalLink.Infrastructure.Persistence;
using MetalLink.Infrastructure.Persistence.Repositories;

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

        // Repositories
        services.AddScoped<ICustomerRepository, CustomerRepository>();

        // Unit of work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
