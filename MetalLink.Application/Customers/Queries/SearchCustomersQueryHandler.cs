using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using MetalLink.Application.Interfaces;
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
        // Just pass the DTO straight through; repository already does wildcard checks
        var customers = await _customerRepository.SearchAsync(request.Request, cancellationToken);

        return customers
            .Select(customer => new CustomerDto
            {
                CustomerId    = customer.CustomerId,
                SiteId        = customer.SiteId,
                FullName      = customer.FullName,
                CompanyName   = customer.CompanyName,
                IdNumber      = customer.IdNumber,
                AccountNumber = customer.AccountNumber,
                PriceCode     = customer.PriceCode,
                AddressLine1  = customer.AddressLine1,
                AddressLine2  = customer.AddressLine2,
                Suburb        = customer.Suburb,
                City          = customer.City,
                PostalCode    = customer.PostalCode,
                PhoneNumber   = customer.PhoneNumber,
                MobileNumber  = customer.MobileNumber,
                Email         = customer.Email
            })
            .ToArray();
    }
}
