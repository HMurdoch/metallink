using System;
using System.Collections.Generic;
using MetalLink.Api.Controllers;
using Xunit;

namespace MetalLink.Tests.Controllers;

public sealed class StockMovementsControllerTimeSeriesTests
{
    [Fact]
    public void BuildBucketEnds_Should_EndExactlyAtTo()
    {
        var from = new DateTimeOffset(2026, 01, 01, 0, 0, 0, TimeSpan.Zero);
        var to = new DateTimeOffset(2026, 02, 01, 0, 0, 0, TimeSpan.Zero);

        var ends = StockMovementsController.BuildBucketEnds(from, to, bucketCount: 80);

        Assert.Equal(80, ends.Length);
        Assert.Equal(to, ends[^1]);
        Assert.All(ends, e => Assert.True(e >= from && e <= to));
    }

    [Fact]
    public void ComputeBucketedSeries_Should_HoldLevelUntilMovementThenJump()
    {
        var from = new DateTimeOffset(2026, 01, 01, 0, 0, 0, TimeSpan.Zero);
        var to = new DateTimeOffset(2026, 01, 11, 0, 0, 0, TimeSpan.Zero);

        var bucketEnds = new[]
        {
            from.AddDays(2),
            from.AddDays(4),
            from.AddDays(6),
            from.AddDays(8),
            to
        };

        var movements = new List<StockMovementsController.MovementRow>
        {
            new() { ProductId = 1, MovementDate = from.AddDays(3), BaseWeightKg = 100, BuyWeightKg = 10, SellWeightKg = 0 }, // -> 110
            new() { ProductId = 1, MovementDate = from.AddDays(7), BaseWeightKg = 110, BuyWeightKg = 0, SellWeightKg = 20 }, // -> 90
        };

        var points = StockMovementsController.ComputeBucketedSeries(bucketEnds, startLevel: 100, movements);

        Assert.Equal(new decimal[] { 100, 110, 110, 90, 90 }, Array.ConvertAll(points, p => p.LevelKg));
    }

    [Theory]
    [InlineData(10, 80)]
    [InlineData(60, 120)]
    [InlineData(200, 160)]
    [InlineData(800, 200)]
    public void ChooseBucketCount_DefaultsByRangeDays(int rangeDays, int expected)
    {
        var from = new DateTimeOffset(2026, 01, 01, 0, 0, 0, TimeSpan.Zero);
        var to = from.AddDays(rangeDays);

        var bc = StockMovementsController.ChooseBucketCount(from, to, requested: null);

        Assert.Equal(expected, bc);
    }

    [Fact]
    public void ChooseBucketCount_ClampsRequested()
    {
        var from = new DateTimeOffset(2026, 01, 01, 0, 0, 0, TimeSpan.Zero);
        var to = from.AddDays(5);

        Assert.Equal(30, StockMovementsController.ChooseBucketCount(from, to, requested: 1));
        Assert.Equal(200, StockMovementsController.ChooseBucketCount(from, to, requested: 1000));
        Assert.Equal(50, StockMovementsController.ChooseBucketCount(from, to, requested: 50));
    }
}
