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

        var addressLine1 = site.AddressLine1 ?? string.Empty;
        var addressLine2 = site.AddressLine2 ?? string.Empty;
        var suburb       = site.Suburb ?? string.Empty;
        var city         = site.City ?? string.Empty;
        var postalCode   = site.PostalCode ?? string.Empty;

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

            Taxable       = customer.Taxable,

            SiteName      = site.SiteName,
            SiteCode      = site.SiteCode,

            AddressLine1  = site.AddressLine1,
            AddressLine2  = site.AddressLine2,
            Suburb        = site.Suburb,
            City          = site.City,
            PostalCode    = site.PostalCode,

            ProvinceId    = site.ProvinceId,
            ProvinceName  = site.Province?.ProvinceName,

            CountryId     = site.CountryId,
            CountryName   = site.Country?.Name,

            IdNumber      = customer.IdNumber,
            AccountNumber = customer.AccountNumber,
            PriceCode     = customer.PriceCode,

            PhoneNumber   = customer.PhoneNumber,
            MobileNumber  = customer.MobileNumber,
            Email         = customer.Email,

            IsActive      = customer.IsActive,
            CreatedTime   = customer.CreatedTime,
            UpdatedTime   = customer.UpdatedTime
        };

        return dto;
    }
}
