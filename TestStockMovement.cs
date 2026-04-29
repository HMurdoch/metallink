using MetalLink.Application.Interfaces;
using MetalLink.Domain.Entities;
using MetalLink.Infrastructure.Persistence;
using MetalLink.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

namespace TestStockMovement
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("TestStockMovement.json")
                .Build();

            var options = new DbContextOptionsBuilder<MetalLinkDbContext>()
                .UseNpgsql(config.GetConnectionString("MetalLinkDatabase"))
                .Options;

            using var db = new MetalLinkDbContext(options);
            var repo = new StockMovementRepository(db);

            try
            {
                // Test inserting a stock movement WITHOUT unit_price_per_kg
                await repo.AddAsync(
                    productId: 1,
                    baseWeightKg: 1000m,
                    buyWeightKg: 500m,
                    sellWeightKg: 0m,
                    unitPricePerKg: 1.50m, // This parameter is still passed but not used in the INSERT
                    createdByOperatorId: 1,
                    notes: "Test stock movement without unit_price_per_kg column",
                    productPriceListId: null,
                    productPriceListProductPriceId: null,
                    receivingTicketId: 1,
                    receivingTicketLineId: 1,
                    sendingTicketId: null,
                    sendingTicketLineId: null
                );

                Console.WriteLine("SUCCESS: Stock movement inserted successfully without unit_price_per_kg!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }
        }
    }
}