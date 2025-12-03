using MediatR;
using MetalLink.Shared.Customers;

namespace MetalLink.Application.Customers.Queries;

public sealed record GetCustomerByIdQuery(long CustomerId) : IRequest<CustomerDto?>;
