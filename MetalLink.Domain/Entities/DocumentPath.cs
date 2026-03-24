using System;

namespace MetalLink.Domain.Entities;

public class DocumentPath
{
    public int DocumentPathId { get; set; }

    public string? CipcDocumentPath { get; set; }
    public string? TradingLicensePath { get; set; }
    public string? VatRegistrationCertificatePath { get; set; }
    public string? TaxClearanceCertificatePath { get; set; }
    public string? BbbeeComplianceCertificatePath { get; set; }

    public int CreatedByOperatorId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedTime { get; set; }
    public DateTimeOffset UpdatedTime { get; set; }
}
