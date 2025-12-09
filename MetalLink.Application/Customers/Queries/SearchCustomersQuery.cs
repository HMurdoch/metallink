using MediatR;
using MetalLink.Shared.Customers;

namespace MetalLink.Application.Customers.Queries;

public sealed record SearchCustomersQuery(CustomerSearchRequestDto Request)
    : IRequest<CustomerDto[]>;
