using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;

namespace SevaOne.Infrastructure.Storage;

public sealed class ObjectStorageOptions
{
    public const string SectionName = "ObjectStorage";

    /// <summary>host:port of an S3-compatible endpoint (MinIO). Empty disables document storage.</summary>
    public string Endpoint { get; set; } = string.Empty;
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string Bucket { get; set; } = "sevaone-documents";
    public bool Secure { get; set; }
}

public interface IObjectStorage
{
    Task PutAsync(string key, byte[] data, string contentType, CancellationToken ct);
    /// <summary>Null when the object does not exist.</summary>
    Task<byte[]?> GetAsync(string key, CancellationToken ct);
}

public sealed class MinioObjectStorage : IObjectStorage
{
    private readonly ObjectStorageOptions _o;
    private readonly IMinioClient _client;
    private readonly SemaphoreSlim _bucketLock = new(1, 1);
    private bool _bucketReady;

    public MinioObjectStorage(IOptions<ObjectStorageOptions> options)
    {
        _o = options.Value;
        _client = new MinioClient()
            .WithEndpoint(string.IsNullOrWhiteSpace(_o.Endpoint) ? "localhost:9000" : _o.Endpoint)
            .WithCredentials(_o.AccessKey, _o.SecretKey)
            .WithSSL(_o.Secure)
            .Build();
    }

    private async Task EnsureBucketAsync(CancellationToken ct)
    {
        if (_bucketReady) return;
        await _bucketLock.WaitAsync(ct);
        try
        {
            if (_bucketReady) return;
            var exists = await _client.BucketExistsAsync(new BucketExistsArgs().WithBucket(_o.Bucket), ct);
            if (!exists) await _client.MakeBucketAsync(new MakeBucketArgs().WithBucket(_o.Bucket), ct);
            _bucketReady = true;
        }
        finally { _bucketLock.Release(); }
    }

    public async Task PutAsync(string key, byte[] data, string contentType, CancellationToken ct)
    {
        await EnsureBucketAsync(ct);
        using var ms = new MemoryStream(data);
        await _client.PutObjectAsync(new PutObjectArgs().WithBucket(_o.Bucket).WithObject(key)
            .WithStreamData(ms).WithObjectSize(data.Length).WithContentType(contentType), ct);
    }

    public async Task<byte[]?> GetAsync(string key, CancellationToken ct)
    {
        await EnsureBucketAsync(ct);
        try
        {
            using var ms = new MemoryStream();
            await _client.GetObjectAsync(new GetObjectArgs().WithBucket(_o.Bucket).WithObject(key)
                .WithCallbackStream(s => s.CopyTo(ms)), ct);
            return ms.ToArray();
        }
        catch (Minio.Exceptions.ObjectNotFoundException) { return null; }
    }
}
