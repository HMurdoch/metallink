using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace MetalLink.Infrastructure.Persistence;

/// <summary>
/// Interceptor that automatically updates the UpdatedTime field on all entity changes
/// This implements Rule 9: Every record modification updates updated_time to now()
/// </summary>
public class AuditInterceptor : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        UpdateAuditFields(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        UpdateAuditFields(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    /// <summary>
    /// Updates the UpdatedTime field for all modified entities
    /// </summary>
    private static void UpdateAuditFields(DbContext? context)
    {
        if (context == null)
            return;

        var now = DateTimeOffset.UtcNow;

        // Get all tracked entities
        var entries = context.ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified)
            .ToList();

        foreach (var entry in entries)
        {
            var entity = entry.Entity;
            var entityType = entity.GetType();

            // Check if entity has UpdatedTime property
            var updatedTimeProperty = entityType.GetProperty("UpdatedTime");
            if (updatedTimeProperty != null && updatedTimeProperty.CanWrite)
            {
                updatedTimeProperty.SetValue(entity, now);
            }
        }
    }
}
