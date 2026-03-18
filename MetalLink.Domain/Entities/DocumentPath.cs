using System;

namespace MetalLink.Domain.Entities;

public class DocumentPath
{
    public int DocumentPathId { get; set; }

    public string? CipcDocumentPath { get; set; }
    public string? TradingLicense { get; set; }
    public string? CiproDocumentPath { get; set; }

    public int CreatedByOperatorId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedTime { get; set; }
    public DateTimeOffset UpdatedTime { get; set; }
}
