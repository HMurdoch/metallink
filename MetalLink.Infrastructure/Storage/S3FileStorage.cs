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
                ForcePathStyle = true
            };

            // For your MinIO service we’ll reuse the same credentials
            // you set in the systemd service: admin / Admin1234!
            _s3Client = new AmazonS3Client("admin", "Admin1234!", config);
        }
        else
        {
            // Real AWS S3 – use IAM roles or AWS creds
            var region = RegionEndpoint.GetBySystemName(_options.Region);
            _s3Client = new AmazonS3Client(region);
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

        if (_options.UseMinio && !string.IsNullOrWhiteSpace(_options.ServiceUrl))
        {
            // MinIO – simple direct URL
            return $"{_options.ServiceUrl.TrimEnd('/')}/{_options.BucketName}/{key}";
        }

        // Real S3 – pre-signed URL
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _options.BucketName,
            Key = key,
            Expires = DateTime.UtcNow.Add(expiresIn.Value),
            Verb = HttpVerb.GET
        };

        return _s3Client.GetPreSignedURL(request);
    }

    public void Dispose()
    {
        _s3Client.Dispose();
    }
}
