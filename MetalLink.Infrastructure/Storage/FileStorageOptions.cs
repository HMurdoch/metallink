namespace MetalLink.Infrastructure.Storage;

public sealed class FileStorageOptions
{
    public string BucketName { get; set; } = string.Empty;
    public string? ServiceUrl { get; set; } // MinIO endpoint, e.g. http://localhost:9000
    public bool UseMinio { get; set; }
    public string Region { get; set; } = "af-south-1";
    public string? AccessKey { get; set; }
    public string? SecretKey { get; set; }
}
