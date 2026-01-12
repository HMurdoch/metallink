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
            Taxable       = request.Taxable,
            IsActive      = true,
            CreatedTime   = now,
            UpdatedTime   = now,
            
            // Image paths
            IdCardImagePath = request.IdCardImagePath,
            DriverLicenseImagePath = request.DriverLicenseImagePath,
            PhotoImagePath = request.PhotoImagePath,
            SignatureImagePath = request.SignatureImagePath,
            FingerprintImagePath = request.FingerprintImagePath
        };

        await _customerRepository.AddAsync(customer, cancellationToken);

        // We only know the Customer row at this stage; Company/Site/Province/Country
        // have not been eagerly loaded here, so we leave those DTO fields null.
        var dto = new CustomerDto
        {
            CustomerId = customer.CustomerId,
            CompanyId  = customer.CompanyId,

            SiteId = customer.SiteId,

            FullName  = customer.FullName,
            FirstName = customer.FirstName,
            LastName  = customer.LastName,
            IsCompany = customer.IsCompany,

            Taxable = customer.Taxable,

            IdNumber      = customer.IdNumber,
            AccountNumber = customer.AccountNumber,
            PriceCode     = customer.PriceCode,
            PhoneNumber   = customer.PhoneNumber,
            MobileNumber  = customer.MobileNumber,
            Email         = customer.Email,

            IsActive    = customer.IsActive,
            CreatedTime = customer.CreatedTime,
            UpdatedTime = customer.UpdatedTime,

            // Image paths
            IdCardImagePath = customer.IdCardImagePath,
            DriverLicenseImagePath = customer.DriverLicenseImagePath,
            PhotoImagePath = customer.PhotoImagePath,
            SignatureImagePath = customer.SignatureImagePath,
            FingerprintImagePath = customer.FingerprintImagePath

            // CompanyName, SiteName, address, Province, Country, etc.
            // are intentionally left null here – they will be filled by
            // search / get-by-id queries that include those navigations.
        };

        return dto;
    }
}
