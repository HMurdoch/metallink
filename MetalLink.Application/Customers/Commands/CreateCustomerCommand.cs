using MediatR;
using MetalLink.Shared.Customers;

namespace MetalLink.Application.Customers.Commands;

public sealed record CreateCustomerCommand : IRequest<CustomerDto?>
{
    /// <summary>
    /// Required for now – every customer is linked to a company.
    /// (We can relax this to long? later if you want truly company-less customers.)
    /// </summary>
    public long CompanyId { get; init; }

    /// <summary>
    /// Optional site / branch.
    /// </summary>
    public long? SiteId { get; init; }

    public string? FirstName { get; init; }
    public string? LastName  { get; init; }
    public bool IsCompany    { get; init; }

    public string? IdNumber      { get; init; }
    public string? AccountNumber { get; init; }
    public string? PriceCode     { get; init; }

    public string? PhoneNumber   { get; init; }
    public string? MobileNumber  { get; init; }
    public string? Email         { get; init; }
}