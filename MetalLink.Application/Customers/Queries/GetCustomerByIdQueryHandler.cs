using MediatR;
using MetalLink.Application.Interfaces;
using MetalLink.Shared.Customers;

using MetalLink.Domain.Entities;

namespace MetalLink.Application.Customers.Queries;

public sealed class GetCustomerByIdQueryHandler
    : IRequestHandler<GetCustomerByIdQuery, CustomerDto?>
{
    private readonly ICustomerRepository _customerRepository;

    public GetCustomerByIdQueryHandler(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public async Task<CustomerDto?> Handle(
        GetCustomerByIdQuery request,
        CancellationToken cancellationToken)
    {
        var customer = await _customerRepository.GetByIdAsync(
            request.CustomerId,
            cancellationToken);

        if (customer == null)
            return null;

        var company = customer.Company ?? new Company();
        var site    = customer.Site ?? new Site();
        var imagePath = customer.ImagePath;

        var dto = new CustomerDto
        {
            CustomerId    = customer.CustomerId,
            CompanyId     = customer.CompanyId,
            SiteId        = customer.SiteId,

            FirstName     = customer.FirstName,
            LastName      = customer.LastName,
            IsCompany     = customer.IsCompany,

            CompanyName   = company.CompanyName,
            VatNumber     = company.VatNumber,

            IsTaxable   = customer.IsTaxable,
            Taxable     = customer.IsTaxable,

            SiteName      = site.SiteName,
            SiteCode      = site.SiteCode,

            IdNumber      = customer.IdNumber,
            AccountNumber = customer.AccountNumber,
            PriceCode     = customer.PriceCode,

            PhoneNumber   = customer.PhoneNumber,
            MobileNumber  = customer.MobileNumber,
            Email         = customer.Email,

            IsActive      = customer.IsActive,
            CreatedTime   = customer.CreatedTime,
            UpdatedTime   = customer.UpdatedTime,

            ImagePathId = customer.ImagePathId,
            IdCardImagePath = imagePath?.IdCardImagePath,
            DriverLicenseImagePath = imagePath?.DriverLicenseImagePath,
            PhotoImagePath = imagePath?.PhotoImagePath,
            SignatureImagePath = imagePath?.SignatureImagePath,
            FingerprintImagePath = imagePath?.FingerprintImagePath
        };

        return dto;
    }
}
