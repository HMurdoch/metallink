using MediatR;
using MetalLink.Shared.Customers;

public sealed record CreateCustomerCommand : IRequest<CustomerDto?>
{
    public long CompanyId { get; init; }
    public long SiteId   { get; init; }

    public string? FirstName { get; init; }
    public string? LastName  { get; init; }
    public bool   IsCompany  { get; init; }

    public string? IdNumber      { get; init; }
    public long? AccountNumber { get; init; }
    public string? PriceCode     { get; init; }

    public string? PhoneNumber   { get; init; }
    public string? MobileNumber  { get; init; }
    public string? Email         { get; init; }

    // NEW – customer-level taxable flag
    public bool Taxable { get; init; } = true;
}
