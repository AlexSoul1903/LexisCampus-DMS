using System.Net.Sockets;
using LexisCampusDMS.Application.Interfaces;
using LexisCampusDMS.Core.Domain.Exceptions;
using LexisCampusDMS.Infraestructure.Shared.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;
using Polly;
using Polly.Retry;

namespace LexisCampusDMS.Infraestructure.Shared.Services;

public class MinioStorageService : IStorageService
{
    private readonly IMinioClient _minioClient;
    private readonly MinioOptions _options;
    private readonly ILogger<MinioStorageService> _logger;
    private readonly AsyncRetryPolicy _retryPolicy;
    private bool _bucketVerified = false;
    private readonly SemaphoreSlim _bucketInitLock = new(1, 1);

    public MinioStorageService(
        IMinioClient minioClient,
        IOptions<MinioOptions> options,
        ILogger<MinioStorageService> logger)
    {
        _minioClient = minioClient ?? throw new ArgumentNullException(nameof(minioClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Exponential backoff resilience policy: 200ms, 400ms, 800ms
        _retryPolicy = Policy
            .Handle<MinioException>()
            .Or<HttpRequestException>()
            .Or<SocketException>()
            .Or<TimeoutException>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt => TimeSpan.FromMilliseconds(Math.Pow(2, attempt) * 100),
                onRetry: (exception, delay, attempt, _) =>
                {
                    _logger.LogWarning(
                        exception,
                        "Transient failure communicating with MinIO on attempt {Attempt}. Retrying in {Delay}ms...",
                        attempt,
                        delay.TotalMilliseconds);
                });
    }

    public async Task<string> UploadFileAsync(
        Stream fileStream,
        string storagePath,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fileStream, nameof(fileStream));
        ArgumentException.ThrowIfNullOrWhiteSpace(storagePath, nameof(storagePath));

        var normalizedKey = StoragePathBuilder.NormalizeKey(storagePath);
        var effectiveContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType;

        await EnsureBucketExistsAsync(cancellationToken);

        try
        {
            await _retryPolicy.ExecuteAsync(async () =>
            {
                // Ensure stream is at position 0 if seekable
                if (fileStream.CanSeek && fileStream.Position != 0)
                {
                    fileStream.Position = 0;
                }

                long streamLength = fileStream.CanSeek ? fileStream.Length : -1;

                var putObjectArgs = new PutObjectArgs()
                    .WithBucket(_options.BucketName)
                    .WithObject(normalizedKey)
                    .WithStreamData(fileStream)
                    .WithObjectSize(streamLength)
                    .WithContentType(effectiveContentType);

                await _minioClient.PutObjectAsync(putObjectArgs, cancellationToken);
            });

            _logger.LogInformation("Successfully uploaded object '{StoragePath}' to bucket '{BucketName}'", normalizedKey, _options.BucketName);
            return normalizedKey;
        }
        catch (Exception ex) when (ex is not DomainException)
        {
            _logger.LogError(ex, "Failed to upload object '{StoragePath}' to MinIO bucket '{BucketName}'", normalizedKey, _options.BucketName);
            throw new DomainValidationException(
                "StorageService", 
                $"No fue posible almacenar el archivo en el servicio de almacenamiento de objetos: {ex.Message}");
        }
    }

    public async Task<Stream> GetFileStreamAsync(
        string storagePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storagePath, nameof(storagePath));

        var normalizedKey = StoragePathBuilder.NormalizeKey(storagePath);
        await EnsureBucketExistsAsync(cancellationToken);

        try
        {
            var memoryStream = new MemoryStream();

            await _retryPolicy.ExecuteAsync(async () =>
            {
                memoryStream.SetLength(0);

                var getObjectArgs = new GetObjectArgs()
                    .WithBucket(_options.BucketName)
                    .WithObject(normalizedKey)
                    .WithCallbackStream(async (stream, ct) =>
                    {
                        await stream.CopyToAsync(memoryStream, ct);
                    });

                await _minioClient.GetObjectAsync(getObjectArgs, cancellationToken);
            });

            memoryStream.Position = 0;
            return memoryStream;
        }
        catch (ObjectNotFoundException ex)
        {
            _logger.LogWarning(ex, "Object '{StoragePath}' was not found in bucket '{BucketName}'", normalizedKey, _options.BucketName);
            throw new EntityNotFoundException("DocumentVersion", normalizedKey);
        }
        catch (Exception ex) when (ex is not DomainException)
        {
            _logger.LogError(ex, "Failed to retrieve object '{StoragePath}' from MinIO", normalizedKey);
            throw new DomainValidationException(
                "StorageService",
                $"Error al descargar el archivo desde el almacenamiento de objetos: {ex.Message}");
        }
    }

    public async Task<bool> DeleteFileAsync(
        string storagePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storagePath, nameof(storagePath));

        var normalizedKey = StoragePathBuilder.NormalizeKey(storagePath);
        await EnsureBucketExistsAsync(cancellationToken);

        try
        {
            await _retryPolicy.ExecuteAsync(async () =>
            {
                var removeObjectArgs = new RemoveObjectArgs()
                    .WithBucket(_options.BucketName)
                    .WithObject(normalizedKey);

                await _minioClient.RemoveObjectAsync(removeObjectArgs, cancellationToken);
            });

            _logger.LogInformation("Deleted object '{StoragePath}' from bucket '{BucketName}'", normalizedKey, _options.BucketName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete object '{StoragePath}' from MinIO", normalizedKey);
            return false;
        }
    }

    public async Task<bool> ExistsAsync(
        string storagePath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(storagePath))
            return false;

        var normalizedKey = StoragePathBuilder.NormalizeKey(storagePath);

        try
        {
            await EnsureBucketExistsAsync(cancellationToken);

            var statObjectArgs = new StatObjectArgs()
                .WithBucket(_options.BucketName)
                .WithObject(normalizedKey);

            var stat = await _minioClient.StatObjectAsync(statObjectArgs, cancellationToken);
            return stat is not null;
        }
        catch (ObjectNotFoundException)
        {
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking existence of object '{StoragePath}' in MinIO", normalizedKey);
            return false;
        }
    }

    private async Task EnsureBucketExistsAsync(CancellationToken cancellationToken)
    {
        if (_bucketVerified) return;

        await _bucketInitLock.WaitAsync(cancellationToken);
        try
        {
            if (_bucketVerified) return;

            var beArgs = new BucketExistsArgs().WithBucket(_options.BucketName);
            var exists = await _minioClient.BucketExistsAsync(beArgs, cancellationToken);

            if (!exists)
            {
                _logger.LogInformation("Provisioning MinIO bucket '{BucketName}'...", _options.BucketName);
                var mbArgs = new MakeBucketArgs().WithBucket(_options.BucketName);
                await _minioClient.MakeBucketAsync(mbArgs, cancellationToken);
                _logger.LogInformation("Bucket '{BucketName}' successfully provisioned.", _options.BucketName);
            }

            _bucketVerified = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify or provision MinIO bucket '{BucketName}'", _options.BucketName);
            throw new DomainValidationException(
                "MinioBucket",
                $"No se pudo conectar o provisionar el bucket de almacenamiento '{_options.BucketName}': {ex.Message}");
        }
        finally
        {
            _bucketInitLock.Release();
        }
    }
}
