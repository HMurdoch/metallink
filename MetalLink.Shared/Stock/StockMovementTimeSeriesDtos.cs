using System;

namespace MetalLink.Shared.Stock;

public sealed class StockMovementTimeSeriesRequestDto
{
    /// <summary>
    /// Product IDs to include. The UI will typically send the 20 products on the current page.
    /// </summary>
    public long[] ProductIds { get; set; } = Array.Empty<long>();

    /// <summary>
    /// When true, the API uses all available history.
    /// </summary>
    public bool AllHistory { get; set; }

    /// <summary>
    /// When true, From is ignored and the range starts at the first available movement ("Day 0").
    /// </summary>
    public bool FromDay0 { get; set; }

    /// <summary>
    /// When true, To is ignored and the range ends at now ("Now").
    /// </summary>
    public bool ToNow { get; set; }

    /// <summary>
    /// Start of range (inclusive) when AllHistory is false and FromDay0 is false.
    /// </summary>
    public DateTimeOffset? From { get; set; }

    /// <summary>
    /// End of range (inclusive) when AllHistory is false and ToNow is false.
    /// </summary>
    public DateTimeOffset? To { get; set; }

    /// <summary>
    /// Optional desired number of buckets (points) across the range.
    /// API will clamp to safe values.
    /// </summary>
    public int? BucketCount { get; set; }
}

public sealed class StockMovementTimeSeriesResponseDto
{
    public int BucketCount { get; set; }
    public DateTimeOffset From { get; set; }
    public DateTimeOffset To { get; set; }

    public StockMovementTimeSeriesProductDto[] Products { get; set; } = Array.Empty<StockMovementTimeSeriesProductDto>();
}

public sealed class StockMovementTimeSeriesProductDto
{
    public long ProductId { get; set; }
    public StockMovementTimeSeriesPointDto[] Points { get; set; } = Array.Empty<StockMovementTimeSeriesPointDto>();
}

public sealed class StockMovementTimeSeriesPointDto
{
    /// <summary>
    /// Bucket end timestamp.
    /// </summary>
    public DateTimeOffset Time { get; set; }

    /// <summary>
    /// Stock-on-hand level (kg) at (or just before) Time.
    /// </summary>
    public decimal LevelKg { get; set; }
}
