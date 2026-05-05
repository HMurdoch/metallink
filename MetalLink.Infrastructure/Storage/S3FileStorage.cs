using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using MetalLink.Application.Interfaces;
using Microsoft.Extensions.Options;

namespace MetalLink.Infrastructure.Storage;

public sealed class S3FileStorage : IFileStorage, IDisposable
{
    private readonly IAmazonS3 _s3Client;
    private readonly FileStorageOptions _options;

    public S3FileStorage(IOptions<FileStorageOptions> options)
    {
        _options = options.Value;

        if (_options.UseMinio && !string.IsNullOrWhiteSpace(_options.ServiceUrl))
        {
            var config = new AmazonS3Config
            {
                ServiceURL = _options.ServiceUrl,
                ForcePathStyle = true,
                UseHttp = _options.ServiceUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            };

            var accessKey = _options.AccessKey ?? "admin";
            var secretKey = _options.SecretKey ?? "Admin1234!";
            _s3Client = new AmazonS3Client(accessKey, secretKey, config);
        }
        else
        {
            var region = RegionEndpoint.GetBySystemName(_options.Region);
            if (!string.IsNullOrWhiteSpace(_options.AccessKey) && !string.IsNullOrWhiteSpace(_options.SecretKey))
            {
                _s3Client = new AmazonS3Client(_options.AccessKey, _options.SecretKey, region);
            }
            else
            {
                _s3Client = new AmazonS3Client(region);
            }
        }
    }

    public async Task<string> UploadAsync(
        byte[] content,
        string contentType,
        string key,
        CancellationToken cancellationToken = default)
    {
        using var stream = new MemoryStream(content);

        var request = new PutObjectRequest
        {
            BucketName = _options.BucketName,
            Key = key,
            InputStream = stream,
            ContentType = contentType
        };

        await _s3Client.PutObjectAsync(request, cancellationToken);

        return key;
    }

    public string GetFileUrl(string key, TimeSpan? expiresIn = null)
    {
        expiresIn ??= TimeSpan.FromHours(1);

        // Generate pre-signed URL for both MinIO and AWS S3
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _options.BucketName,
            Key = key,
            Expires = DateTime.UtcNow.Add(expiresIn.Value),
            Verb = HttpVerb.GET,
            Protocol = _options.UseMinio ? Protocol.HTTP : Protocol.HTTPS
        };

        return _s3Client.GetPreSignedURL(request);
    }

    public void Dispose()
    {
        _s3Client.Dispose();
    }
}
