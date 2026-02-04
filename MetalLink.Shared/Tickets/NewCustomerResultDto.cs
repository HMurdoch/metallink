using System;

namespace MetalLink.Shared.Tickets;

/// <summary>
/// DTO representing a customer that does not have any tickets yet
/// </summary>
public class NewCustomerResultDto
{
    public long CustomerId { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? CompanyName { get; set; }
    public string? SiteName { get; set; }
    public string? AccountNumber { get; set; }
    public DateTimeOffset CreatedTime { get; set; }
}
