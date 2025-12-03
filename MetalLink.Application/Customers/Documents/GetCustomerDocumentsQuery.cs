using MediatR;
using MetalLink.Shared.Customers;

namespace MetalLink.Application.Customers.Documents;

public sealed record GetCustomerDocumentsQuery(long CustomerId)
    : IRequest<IReadOnlyList<CustomerDocumentDto>>;
