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

        if (!await dbContext.Operators.AnyAsync())
        {
            var username = "admin";
            var password = "Admin123!";
            var hash = passwordHasher.HashPassword(password, username);

            var op = new Operator(
                siteId: 1,
                username: username,
                displayName: "System Administrator",
                passwordHash: hash,
                role: "Admin"
            );

            await dbContext.Operators.AddAsync(op);
            await dbContext.SaveChangesAsync();
        }
    }
}
