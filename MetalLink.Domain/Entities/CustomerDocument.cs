namespace MetalLink.Domain.Entities;

public class CustomerDocument
{
    public long CustomerDocumentId { get; private set; }
    public long CustomerId { get; private set; }

    // e.g. "id_front", "id_back", "signature", "fingerprint", "load_photo_top"
    public string DocumentType { get; private set; } = string.Empty;
    public string FileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty; // "image/jpeg", "image/png", etc.
    public string StorageKey { get; private set; } = string.Empty;  // key/path in S3/MinIO

    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedTime { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedTime { get; private set; } = DateTimeOffset.UtcNow;

    private CustomerDocument() { } // EF

    public CustomerDocument(
        long customerId,
        string documentType,
        string fileName,
        string contentType,
        string storageKey)
    {
        CustomerId = customerId;
        DocumentType = documentType;
        FileName = fileName;
        ContentType = contentType;
        StorageKey = storageKey;
        IsActive = true;
        CreatedTime = DateTimeOffset.UtcNow;
        UpdatedTime = DateTimeOffset.UtcNow;
    }
    
    public void Touch()
    {
        UpdatedTime = DateTimeOffset.UtcNow;
    }
    
    public void Deactivate()
    {
        IsActive = false;
        Touch();
    }
}
