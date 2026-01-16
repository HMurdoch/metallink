using System;

namespace MetalLink.Domain.Entities;

public class ImagePath
{
    public int ImagePathId { get; set; }

    public string? IdCardImagePath { get; set; }
    public string? DriverLicenseImagePath { get; set; }
    public string? PhotoImagePath { get; set; }
    public string? SignatureImagePath { get; set; }
    public string? FingerprintImagePath { get; set; }

    public int CreatedByOperatorId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedTime { get; set; }
    public DateTimeOffset UpdatedTime { get; set; }
}
