using System.Threading;
using System.Threading.Tasks;
using MediatR;
using MetalLink.Application.Interfaces;
using MetalLink.Shared.Customers;

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

        var company = customer.Company;
        var site    = customer.Site;

        var addressLine1 = site?.AddressLine1;
        var addressLine2 = site?.AddressLine2;
        var suburb       = site?.Suburb;
        var city         = site?.City;
        var postalCode   = site?.PostalCode;

        var dto = new CustomerDto
        {
            CustomerId    = customer.CustomerId,

            SiteId        = customer.SiteId.HasValue
                ? (int?)checked((int)customer.SiteId.Value)
                : null,

            FullName      = customer.FullName,
            CompanyName   = company?.CompanyName,

            IdNumber      = customer.IdNumber,
            AccountNumber = customer.AccountNumber,
            PriceCode     = customer.PriceCode,

            AddressLine1  = addressLine1,
            AddressLine2  = addressLine2,
            Suburb        = suburb,
            City          = city,
            PostalCode    = postalCode,

            PhoneNumber   = customer.PhoneNumber,
            MobileNumber  = customer.MobileNumber,
            Email         = customer.Email
        };

        return dto;
    }
}
