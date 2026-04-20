using System;
using MetalLink.Infrastructure.Persistence;
using MetalLink.Shared.Stock;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MetalLink.Api.Controllers;

[ApiController]
[Route("api/stock-movements")]
[Authorize]
public sealed class StockMovementsController : ControllerBase
{
    private readonly MetalLinkDbContext _db;

    public StockMovementsController(MetalLinkDbContext db)
    {
        _db = db;
    }

    internal sealed class MovementRow
    {
        public int ProductId { get; set; }
        public DateTimeOffset MovementDate { get; set; }
        public decimal BaseWeightKg { get; set; }
        public decimal BuyWeightKg { get; set; }
        public decimal SellWeightKg { get; set; }
    }

    [HttpPost("time-series")]
    public async Task<ActionResult<StockMovementTimeSeriesResponseDto>> TimeSeries(
        [FromBody] StockMovementTimeSeriesRequestDto req,
        CancellationToken ct)
    {
        if (req is null)
            return BadRequest("Request body required.");

        if (req.ProductIds is null || req.ProductIds.Length == 0)
            return Ok(new StockMovementTimeSeriesResponseDto
            {
                BucketCount = 0,
                From = DateTimeOffset.UtcNow,
                To = DateTimeOffset.UtcNow,
                Products = Array.Empty<StockMovementTimeSeriesProductDto>()
            });

        // De-duplicate and clamp product count for safety.
        var productIds = req.ProductIds
            .Distinct()
            .Take(200) // UI will send ~20; hard cap to prevent abuse.
            .ToArray();

        int[] productIdsInt;
        try
        {
            productIdsInt = productIds.Select(id => checked((int)id)).ToArray();
        }
        catch (OverflowException)
        {
            return BadRequest("One or more productIds are out of range.");
        }

        // Determine range using movement_date.
        DateTimeOffset from;
        DateTimeOffset to;

        // Use explicit parameter for ANY({productIdsInt}) to ensure correct type mapping
        var productIdsParam = new Npgsql.NpgsqlParameter("pIds", productIdsInt);
        var rangeSql = @"
            SELECT
                MIN(created_time) AS ""Min"",
                MAX(created_time) AS ""Max""
            FROM metal_link.stock_movements
            WHERE is_active = true
              AND product_id = ANY(@pIds)
        ";

        var range = await _db.Database.SqlQueryRaw<RangeRow>(rangeSql, productIdsParam).FirstOrDefaultAsync(ct);
        var nowUtc = DateTimeOffset.UtcNow;
        var minTime = range?.Min ?? nowUtc;
        var maxTime = range?.Max ?? nowUtc;

        if (req.AllHistory || (req.FromDay0 && req.ToNow))
        {
            from = minTime;
            to = req.ToNow ? nowUtc : maxTime;
        }
        else
        {
            from = req.FromDay0 ? minTime : (req.From ?? minTime);
            to = req.ToNow ? nowUtc : (req.To ?? maxTime);

            if (to < from)
                return BadRequest("To must be >= From.");
        }

        // PostgreSQL 'timestamp with time zone' requires UTC offset 0 for parameters.
        from = from.ToUniversalTime();
        to = to.ToUniversalTime();

        var bucketCount = ChooseBucketCount(from, to, req.BucketCount);
        if (bucketCount <= 0) bucketCount = 1;
        var bucketEnds = BuildBucketEnds(from, to, bucketCount);

        // Fetch movements using created_time.
        // PostgreSQL 'timestamp with time zone' parameters MUST be UTC.
        // Using UtcDateTime + SqlQueryRaw with explicit parameters is the most robust way to avoid offset errors.
        var toParamObj = new Npgsql.NpgsqlParameter("toParam", to.ToUniversalTime().UtcDateTime);
        var productIdsParam2 = new Npgsql.NpgsqlParameter("pIds", productIdsInt);
        
        var sql = @"
            SELECT
                product_id    AS ""ProductId"",
                created_time  AS ""MovementDate"",
                base_weight_kg AS ""BaseWeightKg"",
                buy_weight_kg  AS ""BuyWeightKg"",
                sell_weight_kg AS ""SellWeightKg""
            FROM metal_link.stock_movements
            WHERE is_active = true
              AND product_id = ANY(@pIds)
              AND created_time <= @toParam
            ORDER BY product_id, created_time
        ";

        var allRows = await _db.Database
            .SqlQueryRaw<MovementRow>(sql, productIdsParam2, toParamObj)
            .ToListAsync(ct);

        Console.WriteLine($"[DEBUG] StockMovementsController.TimeSeries: Found {allRows.Count} total movement rows for {productIdsInt.Length} products in range {from} to {to}.");
        foreach(var row in allRows.Take(5)) {
            Console.WriteLine($"  - Row: Product={row.ProductId}, Time={row.MovementDate}, Base={row.BaseWeightKg}, Buy={row.BuyWeightKg}, Sell={row.SellWeightKg}");
        }

        var products = new List<StockMovementTimeSeriesProductDto>(productIds.Length);

        foreach (var productId in productIds)
        {
            var rows = allRows.Where(r => r.ProductId == productId).ToList();

            // Calculate starting level at 'from'.
            // For stock_movements table, base_weight_kg is typically the weight BEFORE this movement.
            // So Level AFTER movement = base_weight_kg + buy_weight_kg - sell_weight_kg.
            decimal startLevel = 0m;
            var lastBefore = rows.LastOrDefault(r => r.MovementDate <= from);
            if (lastBefore != null)
            {
                startLevel = lastBefore.BaseWeightKg + lastBefore.BuyWeightKg - lastBefore.SellWeightKg;
            }

            // Movements within (from,to]
            var inRange = rows
                .Where(r => r.MovementDate > from && r.MovementDate <= to)
                .ToList();

            var points = ComputeBucketedSeries(bucketEnds, startLevel, inRange);

            products.Add(new StockMovementTimeSeriesProductDto
            {
                ProductId = productId,
                Points = points
            });
        }

        return Ok(new StockMovementTimeSeriesResponseDto
        {
            BucketCount = bucketCount,
            From = from,
            To = to,
            Products = products.ToArray()
        });
    }

    private sealed class RangeRow
    {
        public DateTimeOffset? Min { get; set; }
        public DateTimeOffset? Max { get; set; }
    }


    internal static int ChooseBucketCount(DateTimeOffset from, DateTimeOffset to, int? requested)
    {
        if (requested is not null)
            return Math.Clamp(requested.Value, 30, 200);

        var range = to - from;
        if (range <= TimeSpan.FromDays(31)) return 80;
        if (range <= TimeSpan.FromDays(90)) return 120;
        if (range <= TimeSpan.FromDays(365)) return 160;
        return 200;
    }

    internal static DateTimeOffset[] BuildBucketEnds(DateTimeOffset from, DateTimeOffset to, int bucketCount)
    {
        if (bucketCount <= 1)
            return new[] { to };

        var ticks = (to - from).Ticks;
        if (ticks <= 0)
            return new[] { to };

        var ends = new DateTimeOffset[bucketCount];
        for (var i = 0; i < bucketCount; i++)
        {
            // bucket end at from + ((i+1)/bucketCount)*range
            var t = from + TimeSpan.FromTicks((long)Math.Round(ticks * ((i + 1d) / bucketCount)));
            ends[i] = t;
        }

        // Ensure final bucket end is exactly 'to'.
        ends[^1] = to;
        return ends;
    }

    internal static StockMovementTimeSeriesPointDto[] ComputeBucketedSeries(
        DateTimeOffset[] bucketEnds,
        decimal startLevel,
        List<MovementRow> inRangeOrdered)
    {
        var points = new StockMovementTimeSeriesPointDto[bucketEnds.Length];

        // inRangeOrdered should be time-ordered
        var idx = 0;
        var level = startLevel;

        for (var i = 0; i < bucketEnds.Length; i++)
        {
            var end = bucketEnds[i];

            while (idx < inRangeOrdered.Count && inRangeOrdered[idx].MovementDate <= end)
            {
                var row = inRangeOrdered[idx];
                level = row.BaseWeightKg + row.BuyWeightKg - row.SellWeightKg;
                idx++;
            }

            points[i] = new StockMovementTimeSeriesPointDto
            {
                Time = end,
                LevelKg = level
            };
        }

        return points;
    }
}
