using MediatR;
using MetalLink.Shared.Customers;

namespace MetalLink.Application.Customers.Documents;

public sealed record UploadCustomerDocumentCommand(
    long CustomerId,
    string DocumentType,
    string FileName,
    string ContentType,
    byte[] Content
) : IRequest<CustomerDocumentDto>;
