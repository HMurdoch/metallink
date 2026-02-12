using System;

namespace MetalLink.Shared.Tickets;

/// <summary>
/// DTO representing a buyer that does not have any sending tickets yet.
/// </summary>
public sealed class NewBuyerResultDto
{
    public long BuyerId { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? CompanyName { get; set; }
    public string? SiteName { get; set; }
    public string? AccountNumber { get; set; }
    public DateTimeOffset CreatedTime { get; set; }
}
