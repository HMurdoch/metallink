using System.Threading;
using System.Threading.Tasks;
using MetalLink.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MetalLink.Infrastructure.Persistence;

/// <summary>
/// Generates a globally unique account number across customers and buyers.
/// Uses a PostgreSQL function with an advisory lock to prevent duplicates.
/// </summary>
public sealed class AccountNumberGenerator : IAccountNumberGenerator
{
    private readonly MetalLinkDbContext _db;

    public AccountNumberGenerator(MetalLinkDbContext db)
    {
        _db = db;
    }

    public async Task<long> GetNextAsync(CancellationToken ct = default)
    {
        await EnsureDbFunctionAsync(ct);

        const string sql = "SELECT metal_link.get_next_account_number() AS \"Value\"";
        return await _db.Database.SqlQueryRaw<long>(sql).SingleAsync(ct);
    }

    private async Task EnsureDbFunctionAsync(CancellationToken ct)
    {
        // Create or replace the function. Idempotent.
        // Uses an advisory lock to guarantee uniqueness under concurrency.
        const string ddl = @"
CREATE OR REPLACE FUNCTION metal_link.get_next_account_number()
RETURNS bigint
LANGUAGE plpgsql
AS $$
DECLARE
    v_next bigint;
BEGIN
    PERFORM pg_advisory_lock(987654321);

    SELECT GREATEST(
        COALESCE((SELECT MAX(account_number) FROM metal_link.customers), 0),
        COALESCE((SELECT MAX(account_number) FROM metal_link.buyers), 0)
    ) + 1
    INTO v_next;

    PERFORM pg_advisory_unlock(987654321);

    RETURN v_next;
END;
$$;
";

        await _db.Database.ExecuteSqlRawAsync(ddl, ct);
    }
}
