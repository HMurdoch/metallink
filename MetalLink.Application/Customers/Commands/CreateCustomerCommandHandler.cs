using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using MetalLink.Application.Interfaces;
using MetalLink.Domain.Entities;
using MetalLink.Shared.Customers;

namespace MetalLink.Application.Customers.Commands;

public sealed class CreateCustomerCommandHandler
    : IRequestHandler<CreateCustomerCommand, CustomerDto?>
{
    private readonly ICustomerRepository _customerRepository;

    public CreateCustomerCommandHandler(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public async Task<CustomerDto?> Handle(
        CreateCustomerCommand request,
        CancellationToken cancellationToken)
    {
        // Minimal validation – you can tighten this if you want:
        if (string.IsNullOrWhiteSpace(request.FirstName) &&
            string.IsNullOrWhiteSpace(request.LastName))
        {
            throw new ArgumentException("First or last name is required.");
        }

        // You may want to validate request.CompanyId / SiteId here as well.

        var now = DateTime.UtcNow;

        var customer = new Customer
        {
            CompanyId     = request.CompanyId,
            SiteId        = request.SiteId,
            FirstName     = request.FirstName,
            LastName      = request.LastName,
            IsCompany     = request.IsCompany,
            IdNumber      = request.IdNumber,
            AccountNumber = request.AccountNumber,
            PriceCode     = request.PriceCode,
            PhoneNumber   = request.PhoneNumber,
            MobileNumber  = request.MobileNumber,
            Email         = request.Email,
            IsActive      = true,
            CreatedTime   = now,
            UpdatedTime   = now
        };

        await _customerRepository.AddAsync(customer, cancellationToken);

        // Build DTO – address now lives on Company/Site, so we leave those null for now.
        var dto = new CustomerDto
        {
            CustomerId    = customer.CustomerId,
            SiteId        = customer.SiteId,
            FullName      = customer.FullName,
            CompanyName   = null,           // can be filled from Company later if needed
            IdNumber      = customer.IdNumber,
            AccountNumber = customer.AccountNumber,
            PriceCode     = customer.PriceCode,
            AddressLine1  = null,
            AddressLine2  = null,
            Suburb        = null,
            City          = null,
            PostalCode    = null,
            PhoneNumber   = customer.PhoneNumber,
            MobileNumber  = customer.MobileNumber,
            Email         = customer.Email
        };

        return dto;
    }
}
