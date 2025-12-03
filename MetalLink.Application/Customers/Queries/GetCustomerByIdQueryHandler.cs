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
        var customer = await _customerRepository.GetByIdAsync(request.CustomerId, cancellationToken);

        if (customer == null)
        {
            return null;
        }

        return new CustomerDto
        {
            CustomerId = customer.CustomerId,
            SiteId = customer.SiteId,
            FullName = customer.FullName,
            IsCompany = customer.IsCompany,
            CompanyName = customer.CompanyName,
            IdNumber = customer.IdNumber,
            AccountNumber = customer.AccountNumber,
            PriceCode = customer.PriceCode,
            AddressLine1 = customer.AddressLine1,
            AddressLine2 = customer.AddressLine2,
            Suburb = customer.Suburb,
            City = customer.City,
            PostalCode = customer.PostalCode,
            PhoneNumber = customer.PhoneNumber,
            MobileNumber = customer.MobileNumber,
            Email = customer.Email
        };
    }
}
