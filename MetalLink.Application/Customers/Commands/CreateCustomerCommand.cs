using MediatR;
using MetalLink.Shared.Customers;

namespace MetalLink.Application.Customers.Commands;

public sealed record CreateCustomerCommand(
    long SiteId,
    string FullName,
    bool IsCompany,
    string? CompanyName,
    string? IdNumber,
    string? AccountNumber,
    string? PriceCode,
    string? AddressLine1,
    string? AddressLine2,
    string? Suburb,
    string? City,
    string? PostalCode,
    string? PhoneNumber,
    string? MobileNumber,
    string? Email
) : IRequest<CustomerDto>;
