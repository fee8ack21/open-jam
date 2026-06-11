using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;
using StorageService.Options;

namespace StorageService.Storage;

/// <summary>以 MinIO（S3 相容）實作的儲存後端，用於本地開發環境。</summary>
public class MinioStorageProvider(IMinioClient minio, IOptions<StorageOptions> options) : IStorageProvider
{
    private readonly StorageOptions _opts = options.Value;

    /// <inheritdoc/>
    public async Task<string> GenerateUploadUrlAsync(string key, string contentType, long maxBytes, TimeSpan expiry, CancellationToken ct = default)
    {
        var args = new PresignedPutObjectArgs()
            .WithBucket(_opts.Bucket)
            .WithObject(key)
            .WithExpiry((int)expiry.TotalSeconds);

        return await minio.PresignedPutObjectAsync(args);
    }

    /// <inheritdoc/>
    public async Task<string> GenerateDownloadUrlAsync(string key, TimeSpan expiry, CancellationToken ct = default)
    {
        var args = new PresignedGetObjectArgs()
            .WithBucket(_opts.Bucket)
            .WithObject(key)
            .WithExpiry((int)expiry.TotalSeconds);

        return await minio.PresignedGetObjectAsync(args);
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(string key, CancellationToken ct = default)
    {
        var args = new RemoveObjectArgs()
            .WithBucket(_opts.Bucket)
            .WithObject(key);

        await minio.RemoveObjectAsync(args, ct);
    }

    /// <inheritdoc/>
    public async Task<bool> ObjectExistsAsync(string key, CancellationToken ct = default)
    {
        try
        {
            var args = new StatObjectArgs()
                .WithBucket(_opts.Bucket)
                .WithObject(key);

            await minio.StatObjectAsync(args, ct);
            return true;
        }
        catch (ObjectNotFoundException)
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task EnsurePublicReadPolicyAsync(CancellationToken ct = default)
    {
        var policy = $$"""
            {
              "Version": "2012-10-17",
              "Statement": [
                {
                  "Effect": "Allow",
                  "Principal": {"AWS": ["*"]},
                  "Action": ["s3:GetObject"],
                  "Resource": ["arn:aws:s3:::{{_opts.Bucket}}/public/*"]
                }
              ]
            }
            """;

        var args = new SetPolicyArgs()
            .WithBucket(_opts.Bucket)
            .WithPolicy(policy);

        await minio.SetPolicyAsync(args, ct);
    }
}
