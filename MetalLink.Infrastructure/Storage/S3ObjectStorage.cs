using Amazon.S3;
using Amazon.S3.Model;
using MetalLink.Application.Storage;
using Microsoft.Extensions.Configuration;

namespace MetalLink.Infrastructure.Storage;

public class S3ObjectStorage : IObjectStorage
{
    private readonly IAmazonS3 _s3;
    private readonly string _bucketName;

    public S3ObjectStorage(IAmazonS3 s3, IConfiguration configuration)
    {
        _s3 = s3;
        _bucketName = configuration.GetSection("S3")["BucketName"] 
                      ?? throw new InvalidOperationException("S3:BucketName not configured");
    }

    public async Task UploadAsync(string key, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        var request = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = key,
            InputStream = content,
            ContentType = contentType
        };

        await _s3.PutObjectAsync(request, cancellationToken);
    }

    public async Task<Stream> DownloadAsync(string key, CancellationToken cancellationToken = default)
    {
        var response = await _s3.GetObjectAsync(_bucketName, key, cancellationToken);
        var ms = new MemoryStream();
        await response.ResponseStream.CopyToAsync(ms, cancellationToken);
        ms.Position = 0;
        return ms;
    }

    public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
        => _s3.DeleteObjectAsync(_bucketName, key, cancellationToken);

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await _s3.GetObjectMetadataAsync(_bucketName, key, cancellationToken);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }
}
