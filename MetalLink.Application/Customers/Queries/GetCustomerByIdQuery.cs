using MediatR;
using MetalLink.Shared.Customers;

namespace MetalLink.Application.Customers.Queries;

public sealed record GetCustomerByIdQuery(int CustomerId) : IRequest<CustomerDto?>;
