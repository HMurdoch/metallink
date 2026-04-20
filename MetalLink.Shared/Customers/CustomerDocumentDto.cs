namespace MetalLink.Shared.Customers;

public sealed class CustomerDocumentDto
{
    public int CustomerDocumentId { get; set; }
    public int CustomerId { get; set; }

    public string DocumentType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string StorageKey { get; set; } = string.Empty;

    public string? Url { get; set; } // pre-signed or direct link
    public DateTimeOffset CreatedTime { get; set; }
}
