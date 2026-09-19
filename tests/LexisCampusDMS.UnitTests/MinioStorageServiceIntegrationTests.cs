using System.Text;
using LexisCampusDMS.Infraestructure.Shared.Options;
using LexisCampusDMS.Infraestructure.Shared.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Minio;
using Xunit;

namespace LexisCampusDMS.UnitTests.Shared;

public class MinioStorageServiceIntegrationTests
{
    private readonly MinioStorageService _storageService;
    private readonly Sha256HashService _hashService;

    public MinioStorageServiceIntegrationTests()
    {
        var options = Options.Create(new MinioOptions
        {
            Endpoint = "localhost:9000",
            AccessKey = "admin",
            SecretKey = "Admin@LexisCampus",
            BucketName = "lexiscampus-docs",
            UseSsl = false
        });

        var minioClient = new MinioClient()
            .WithEndpoint("localhost:9000")
            .WithCredentials("admin", "Admin@LexisCampus")
            .Build();

        _storageService = new MinioStorageService(
            minioClient,
            options,
            NullLogger<MinioStorageService>.Instance);

        _hashService = new Sha256HashService();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task MinioStorageService_FullLifecycle_UploadDownloadExistsDelete()
    {
        // Skip gracefully if local MinIO Docker container is not running on localhost:9000
        if (!await IsMinioAvailableAsync())
        {
            return;
        }

        // 1. Arrange
        var testContent = "LexisCampus DMS - Document Content Integrity Test " + Guid.NewGuid();
        var fileBytes = Encoding.UTF8.GetBytes(testContent);
        using var uploadStream = new MemoryStream(fileBytes);

        var hash = _hashService.ComputeSha256(uploadStream);
        var storagePath = StoragePathBuilder.BuildPath("2023-0145", "test-document.pdf", hash, 2026);

        // 2. Act - Upload
        var uploadedKey = await _storageService.UploadFileAsync(uploadStream, storagePath, "application/pdf");
        Assert.Equal(storagePath, uploadedKey);

        // 3. Act - Exists
        var exists = await _storageService.ExistsAsync(storagePath);
        Assert.True(exists, "Object should exist in MinIO after upload");

        // 4. Act - Download and verify bytes
        using var downloadedStream = await _storageService.GetFileStreamAsync(storagePath);
        using var reader = new StreamReader(downloadedStream, Encoding.UTF8);
        var downloadedContent = await reader.ReadToEndAsync();
        Assert.Equal(testContent, downloadedContent);

        // 5. Act - Delete
        var deleted = await _storageService.DeleteFileAsync(storagePath);
        Assert.True(deleted, "Delete operation should return true");

        // 6. Act - Verify Exists after deletion
        var existsAfterDelete = await _storageService.ExistsAsync(storagePath);
        Assert.False(existsAfterDelete, "Object should no longer exist after deletion");
    }

    private static async Task<bool> IsMinioAvailableAsync()
    {
        try
        {
            using var tcpClient = new System.Net.Sockets.TcpClient();
            var connectTask = tcpClient.ConnectAsync("localhost", 9000);
            var delayTask = Task.Delay(500);
            var completedTask = await Task.WhenAny(connectTask, delayTask);
            return completedTask == connectTask && tcpClient.Connected;
        }
        catch
        {
            return false;
        }
    }
}
