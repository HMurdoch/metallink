using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using MetalLink.Application.Interfaces;
using MetalLink.Domain.Entities;
using MetalLink.Shared.Customers;

namespace MetalLink.Application.Customers.Queries;

public sealed class SearchCustomersQueryHandler
    : IRequestHandler<SearchCustomersQuery, CustomerDto[]>
{
    private readonly ICustomerRepository _customerRepository;

    public SearchCustomersQueryHandler(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public async Task<CustomerDto[]> Handle(
        SearchCustomersQuery request,
        CancellationToken cancellationToken)
    {
        var r = request.Request;

        // We now use the repository SearchAsync(CustomerSearchRequestDto)
        var customers = await _customerRepository.SearchAsync(r, cancellationToken);

        // Project Domain.Customer (+ Company/Site) into the flattened CustomerDto
        var result = customers.Select(c =>
        {
            // Prefer site address; fall back to company address if needed
            var company = c.Company;
            var site    = c.Site;

            var addressLine1 = site?.AddressLine1;
            var addressLine2 = site?.AddressLine2;
            var suburb       = site?.Suburb;
            var city         = site?.City;
            var postalCode   = site?.PostalCode;

            return new CustomerDto
            {
                CustomerId    = c.CustomerId,

                // DTO still uses int? for SiteId – cast from long?
                SiteId        = c.SiteId.HasValue
                    ? (int?)checked((int)c.SiteId.Value)
                    : null,

                FullName      = c.FullName,
                CompanyName   = company?.CompanyName,

                IdNumber      = c.IdNumber,
                AccountNumber = c.AccountNumber,
                PriceCode     = c.PriceCode,

                AddressLine1  = addressLine1,
                AddressLine2  = addressLine2,
                Suburb        = suburb,
                City          = city,
                PostalCode    = postalCode,

                PhoneNumber   = c.PhoneNumber,
                MobileNumber  = c.MobileNumber,
                Email         = c.Email
            };
        })
        .ToArray();

        return result;
    }
}
