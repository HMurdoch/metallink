using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using MetalLink.Application.Services;

namespace MetalLink.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // MediatR handlers in Application
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
        });

        // FluentValidation validators in Application
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        // Custom application services
        services.AddScoped<TicketNumberService>();
        services.AddScoped<PriceLookupService>();
        services.AddScoped<WeightCalculationService>();
        services.AddScoped<TicketCalculationService>();

        return services;
    }
}
