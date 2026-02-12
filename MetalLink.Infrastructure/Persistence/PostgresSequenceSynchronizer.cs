using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace MetalLink.Infrastructure.Persistence;

/// <summary>
/// Utility to re-sync Postgres sequences/identity columns to the current MAX(id).
///
/// This fixes issues where data was imported/restored and the underlying sequence
/// values were not updated, causing inserts to fail with duplicate key violations.
/// </summary>
public static class PostgresSequenceSynchronizer
{
    public static async Task SyncAllIdentitySequencesAsync(
        DbContext dbContext,
        CancellationToken cancellationToken = default)
    {
        if (dbContext.Database.ProviderName is null ||
            !dbContext.Database.ProviderName.Contains("Npgsql", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        // Find all sequences used by table columns (SERIAL/IDENTITY).
        //
        // Primary method: pg_depend ownership relationship (works for properly created SERIAL/IDENTITY).
        // Fallback: parse column DEFAULT expression (nextval('...'::regclass)).
        //
        // Some restores/imports end up with a column default pointing to a sequence but without the
        // dependency link, which makes pg_depend-based discovery miss it.
        const string discoverySql = """
            WITH owned_sequences AS (
                SELECT
                    n.nspname  AS schema_name,
                    c.relname  AS table_name,
                    a.attname  AS column_name,
                    format('%I.%I', ns.nspname, seq.relname) AS sequence_name
                FROM pg_class c
                JOIN pg_namespace n ON n.oid = c.relnamespace
                JOIN pg_attribute a ON a.attrelid = c.oid
                                   AND a.attnum > 0
                                   AND NOT a.attisdropped
                JOIN pg_depend dep ON dep.refobjid = c.oid
                                  AND dep.refobjsubid = a.attnum
                                  AND dep.deptype IN ('a', 'i')
                JOIN pg_class seq ON seq.oid = dep.objid
                                 AND seq.relkind = 'S'
                JOIN pg_namespace ns ON ns.oid = seq.relnamespace
                WHERE c.relkind IN ('r', 'p')
                  AND n.nspname NOT IN ('pg_catalog', 'information_schema')
            ),
            default_sequences AS (
                SELECT
                    n.nspname  AS schema_name,
                    c.relname  AS table_name,
                    a.attname  AS column_name,
                    substring(pg_get_expr(ad.adbin, ad.adrelid)
                        FROM 'nextval\(''([^'']+)''::regclass\)') AS sequence_name
                FROM pg_class c
                JOIN pg_namespace n ON n.oid = c.relnamespace
                JOIN pg_attribute a ON a.attrelid = c.oid
                                   AND a.attnum > 0
                                   AND NOT a.attisdropped
                JOIN pg_attrdef ad ON ad.adrelid = c.oid AND ad.adnum = a.attnum
                WHERE c.relkind IN ('r', 'p')
                  AND n.nspname NOT IN ('pg_catalog', 'information_schema')
                  AND pg_get_expr(ad.adbin, ad.adrelid) LIKE 'nextval(%'
            )
            SELECT
                schema_name  AS "SchemaName",
                table_name   AS "TableName",
                column_name  AS "ColumnName",
                sequence_name AS "SequenceName"
            FROM (
                SELECT * FROM owned_sequences
                UNION
                SELECT * FROM default_sequences
            ) s
            WHERE sequence_name IS NOT NULL AND sequence_name <> ''
            ORDER BY schema_name, table_name, column_name;
            """;

        var sequenceColumns = await dbContext.Database
            .SqlQueryRaw<SequenceColumn>(discoverySql)
            .ToListAsync(cancellationToken);

        foreach (var sc in sequenceColumns.DistinctBy(x => x.SequenceName))
        {
            if (string.IsNullOrWhiteSpace(sc.SequenceName))
                continue;

            // setval(seq, max(id), true) => nextval returns max(id)+1
            // When the table is empty, MAX(id) is NULL. We must NOT set the sequence to 0 because
            // many sequences have a minimum value of 1 (and Postgres will throw).
            // Instead, set it to 1 with is_called=false so that nextval returns 1.
            //
            // We cannot parameterize identifiers here, so we quote schema/table/column names.
            var setValSql = $"""
                SELECT CASE
                    WHEN (SELECT MAX("{sc.ColumnName}") FROM "{sc.SchemaName}"."{sc.TableName}") IS NULL
                        THEN setval('{EscapeSqlLiteral(sc.SequenceName)}'::regclass, 1, false)
                    ELSE setval(
                        '{EscapeSqlLiteral(sc.SequenceName)}'::regclass,
                        (SELECT MAX("{sc.ColumnName}") FROM "{sc.SchemaName}"."{sc.TableName}"),
                        true
                    )
                END;
                """;

            await dbContext.Database.ExecuteSqlRawAsync(setValSql, cancellationToken);
        }
    }

    private static string EscapeSqlLiteral(string value) => value.Replace("'", "''");

    private sealed record SequenceColumn(
        string SchemaName,
        string TableName,
        string ColumnName,
        string? SequenceName);
}
