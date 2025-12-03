namespace MetalLink.Application.Interfaces;

public interface IFileStorage
{
    /// <summary>
    /// Uploads a file to storage and returns the storage key/path.
    /// </summary>
    Task<string> UploadAsync(
        byte[] content,
        string contentType,
        string key,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a URL (direct or pre-signed) to access the file.
    /// </summary>
    string GetFileUrl(string key, TimeSpan? expiresIn = null);
}
