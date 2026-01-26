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
    private readonly IAccountNumberGenerator _accountNumberGenerator;

    public CreateCustomerCommandHandler(
        ICustomerRepository customerRepository,
        IAccountNumberGenerator accountNumberGenerator)
    {
        _customerRepository = customerRepository;
        _accountNumberGenerator = accountNumberGenerator;
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

        var now = DateTimeOffset.UtcNow;

        var accountNumber = request.AccountNumber ?? await _accountNumberGenerator.GetNextAsync(cancellationToken);

        var customer = new Customer
        {
            CompanyId     = request.CompanyId,
            SiteId        = request.SiteId,
            FirstName     = request.FirstName,
            LastName      = request.LastName,
            IsCompany     = request.IsCompany,
            IdNumber      = request.IdNumber,
            AccountNumber = accountNumber,
            PriceCode     = request.PriceCode,
            PhoneNumber   = request.PhoneNumber,
            MobileNumber  = request.MobileNumber,
            Email         = request.Email,
            IsTaxable       = request.IsTaxable,
            IsActive      = true,
            CreatedTime   = now,
            UpdatedTime   = now,
            ImagePathId = request.ImagePathId,
            CreatedByOperatorId = request.CreatedByOperatorId
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

            IsTaxable = customer.IsTaxable,
            Taxable   = customer.IsTaxable,

            IdNumber      = customer.IdNumber,
            AccountNumber = customer.AccountNumber,
            PriceCode     = customer.PriceCode,
            PhoneNumber   = customer.PhoneNumber,
            MobileNumber  = customer.MobileNumber,
            Email         = customer.Email,

            IsActive    = customer.IsActive,
            CreatedTime = customer.CreatedTime,
            UpdatedTime = customer.UpdatedTime,
            ImagePathId = customer.ImagePathId

            // CompanyName, SiteName, address, Province, Country, etc.
            // are intentionally left null here – they will be filled by
            // search / get-by-id queries that include those navigations.
        };

        return dto;
    }
}
