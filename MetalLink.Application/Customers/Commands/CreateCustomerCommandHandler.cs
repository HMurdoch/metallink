using MediatR;
using MetalLink.Application.Interfaces;
using MetalLink.Domain.Entities;
using MetalLink.Shared.Customers;

namespace MetalLink.Application.Customers.Commands;

public sealed class CreateCustomerCommandHandler
    : IRequestHandler<CreateCustomerCommand, CustomerDto>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateCustomerCommandHandler(
        ICustomerRepository customerRepository,
        IUnitOfWork unitOfWork)
    {
        _customerRepository = customerRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CustomerDto> Handle(
        CreateCustomerCommand request,
        CancellationToken cancellationToken)
    {
        // Optional: uniqueness check on account number
        if (!string.IsNullOrWhiteSpace(request.AccountNumber))
        {
            var existing = await _customerRepository
                .GetByAccountNumberAsync(request.AccountNumber, cancellationToken);

            if (existing != null)
            {
                throw new InvalidOperationException(
                    $"A customer with account number '{request.AccountNumber}' already exists.");
            }
        }

        var customer = new Customer(
            request.SiteId,
            request.FullName,
            request.IsCompany,
            request.CompanyName
        );

        customer.SetIdentity(
            request.IdNumber,
            request.AccountNumber,
            request.PriceCode
        );

        customer.SetAddress(
            request.AddressLine1,
            request.AddressLine2,
            request.Suburb,
            request.City,
            request.PostalCode
        );

        customer.SetContact(
            request.PhoneNumber,
            request.MobileNumber,
            request.Email
        );

        await _customerRepository.AddAsync(customer, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

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
