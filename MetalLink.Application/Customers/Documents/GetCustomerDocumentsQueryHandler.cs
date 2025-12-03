using MediatR;
using MetalLink.Application.Interfaces;
using MetalLink.Shared.Customers;

namespace MetalLink.Application.Customers.Documents;

public sealed class GetCustomerDocumentsQueryHandler
    : IRequestHandler<GetCustomerDocumentsQuery, IReadOnlyList<CustomerDocumentDto>>
{
    private readonly ICustomerDocumentRepository _documentRepository;
    private readonly IFileStorage _fileStorage;

    public GetCustomerDocumentsQueryHandler(
        ICustomerDocumentRepository documentRepository,
        IFileStorage fileStorage)
    {
        _documentRepository = documentRepository;
        _fileStorage = fileStorage;
    }

    public async Task<IReadOnlyList<CustomerDocumentDto>> Handle(
        GetCustomerDocumentsQuery request,
        CancellationToken cancellationToken)
    {
        var documents = await _documentRepository.GetByCustomerIdAsync(
            request.CustomerId,
            cancellationToken);

        var result = documents
            .Select(d => new CustomerDocumentDto
            {
                CustomerDocumentId = d.CustomerDocumentId,
                CustomerId = d.CustomerId,
                DocumentType = d.DocumentType,
                FileName = d.FileName,
                ContentType = d.ContentType,
                StorageKey = d.StorageKey,
                Url = _fileStorage.GetFileUrl(d.StorageKey, TimeSpan.FromHours(1)),
                CreatedTime = d.CreatedTime
            })
            .ToList()
            .AsReadOnly();

        return result;
    }
}
