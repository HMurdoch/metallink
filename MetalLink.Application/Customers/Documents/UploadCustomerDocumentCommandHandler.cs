using MediatR;
using MetalLink.Application.Interfaces;
using MetalLink.Domain.Entities;
using MetalLink.Shared.Customers;

namespace MetalLink.Application.Customers.Documents;

public sealed class UploadCustomerDocumentCommandHandler
    : IRequestHandler<UploadCustomerDocumentCommand, CustomerDocumentDto>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly ICustomerDocumentRepository _documentRepository;
    private readonly IFileStorage _fileStorage;
    private readonly IUnitOfWork _unitOfWork;

    public UploadCustomerDocumentCommandHandler(
        ICustomerRepository customerRepository,
        ICustomerDocumentRepository documentRepository,
        IFileStorage fileStorage,
        IUnitOfWork unitOfWork)
    {
        _customerRepository = customerRepository;
        _documentRepository = documentRepository;
        _fileStorage = fileStorage;
        _unitOfWork = unitOfWork;
    }

    public async Task<CustomerDocumentDto> Handle(UploadCustomerDocumentCommand request, CancellationToken cancellationToken)
    {
        var customer = await _customerRepository.GetByIdAsync(request.CustomerId, cancellationToken);
        if (customer == null)
        {
            throw new InvalidOperationException($"Customer {request.CustomerId} not found.");
        }

        // Key: customers/{customer_id}/{document_type}/{timestamp}_{filename}
        var safeFileName = request.FileName.Replace(" ", "_");
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var key = $"customers/{request.CustomerId}/{request.DocumentType}/{timestamp}_{safeFileName}";

        // Upload bytes to MinIO/S3
        var storageKey = await _fileStorage.UploadAsync(
            request.Content,
            request.ContentType,
            key,
            cancellationToken
        );

        var customerDocument = new CustomerDocument(
            request.CustomerId,
            request.DocumentType,
            request.FileName,
            request.ContentType,
            storageKey
        );

        await _documentRepository.AddAsync(customerDocument, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var url = _fileStorage.GetFileUrl(storageKey, TimeSpan.FromHours(1));

        return new CustomerDocumentDto
        {
            CustomerDocumentId = customerDocument.CustomerDocumentId,
            CustomerId = customerDocument.CustomerId,
            DocumentType = customerDocument.DocumentType,
            FileName = customerDocument.FileName,
            ContentType = customerDocument.ContentType,
            StorageKey = customerDocument.StorageKey,
            Url = url,
            CreatedTime = customerDocument.CreatedTime
        };
    }
}
