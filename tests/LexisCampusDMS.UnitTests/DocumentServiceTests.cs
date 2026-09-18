using System.Text;
using LexisCampusDMS.Application.DTOs;
using LexisCampusDMS.Application.Interfaces;
using LexisCampusDMS.Application.Services;
using LexisCampusDMS.Application.Validators;
using LexisCampusDMS.Core.Domain.Entities;
using LexisCampusDMS.Core.Domain.Enums;
using LexisCampusDMS.Core.Domain.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace LexisCampusDMS.UnitTests.Application;

public class DocumentServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IDocumentRepository> _documentRepoMock = new();
    private readonly Mock<IStorageService> _storageServiceMock = new();
    private readonly Mock<IHashService> _hashServiceMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();
    private readonly Mock<IGenericRepository<AuditLog, Guid>> _auditRepoMock = new();
    private readonly UploadDocumentRequestDtoValidator _validator = new();

    private readonly DocumentService _documentService;

    public DocumentServiceTests()
    {
        _unitOfWorkMock.Setup(u => u.Repository<AuditLog, Guid>()).Returns(_auditRepoMock.Object);

        _currentUserMock.Setup(u => u.UserId).Returns("usr-12345");
        _currentUserMock.Setup(u => u.IpAddress).Returns("192.168.1.100");

        _documentService = new DocumentService(
            _unitOfWorkMock.Object,
            _documentRepoMock.Object,
            _storageServiceMock.Object,
            _hashServiceMock.Object,
            _currentUserMock.Object,
            _validator,
            NullLogger<DocumentService>.Instance);
    }

    [Fact]
    public async Task UploadDocumentAsync_ValidRequest_CalculatesHashUploadsToMinioAndPersistsTransactionally()
    {
        // Arrange
        var content = "PDF Document Mock Content";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        var expectedHash = "a1b2c3d4e5f678901234567890abcdef1234567890abcdef1234567890abcdef";

        _hashServiceMock
            .Setup(h => h.ComputeSha256Async(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedHash);

        _storageServiceMock
            .Setup(s => s.UploadFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("2023-0145/2026/test_Certificado.pdf");

        var request = new UploadDocumentRequestDto
        {
            Title = "Certificado de Estudios 2026",
            StudentRegistration = "2023-0145",
            DocumentType = DocumentType.StudyCertificate,
            FileName = "Certificado.pdf",
            ContentType = "application/pdf",
            FileStream = stream,
            FileSizeBytes = stream.Length
        };

        // Act
        var result = await _documentService.UploadDocumentAsync(request);

        // Assert: 1. Success result
        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal("Certificado de Estudios 2026", result.Data.Title);
        Assert.Equal("2023-0145", result.Data.StudentRegistration);
        Assert.Equal(1, result.Data.CurrentVersion);
        Assert.Equal(expectedHash, result.Data.CurrentFileHash);

        // Assert: 2. Storage upload was called
        _storageServiceMock.Verify(s => s.UploadFileAsync(
            stream, 
            It.Is<string>(path => path.Contains("2023-0145") && path.Contains(expectedHash)), 
            "application/pdf", 
            It.IsAny<CancellationToken>()), Times.Once);

        // Assert: 3. DB Transaction and Document registration
        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _documentRepoMock.Verify(d => d.RegisterDocumentAsync(
            It.Is<Document>(doc => doc.Title == request.Title && doc.StudentRegistration == "2023-0145"), 
            It.IsAny<CancellationToken>()), Times.Once);

        // Assert: 4. AuditLog registration with DOCUMENT_UPLOADED action
        _auditRepoMock.Verify(a => a.AddAsync(
            It.Is<AuditLog>(log => log.Action == AuditAction.DocumentUploaded && log.UserId == "usr-12345"), 
            It.IsAny<CancellationToken>()), Times.Once);

        _unitOfWorkMock.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UploadDocumentAsync_InvalidMetadata_ReturnsFailureWithoutCallingStorageOrDb()
    {
        // Arrange: Missing title and empty file
        var request = new UploadDocumentRequestDto
        {
            Title = "", // Invalid
            StudentRegistration = "", // Invalid
            FileName = "",
            FileStream = Stream.Null,
            FileSizeBytes = 0
        };

        // Act
        var result = await _documentService.UploadDocumentAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_ERROR", result.ErrorCode);
        Assert.NotEmpty(result.Errors);

        _hashServiceMock.Verify(h => h.ComputeSha256Async(It.IsAny<Stream>(), It.IsAny<CancellationToken>()), Times.Never);
        _storageServiceMock.Verify(s => s.UploadFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UploadDocumentAsync_DatabaseFailure_RollsBackAndDeletesStorageBlob()
    {
        // Arrange
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("Test Stream"));
        var expectedHash = "hash12345";
        var expectedPath = "2023-0145/2026/hash12345_Record.pdf";

        _hashServiceMock
            .Setup(h => h.ComputeSha256Async(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedHash);

        _storageServiceMock
            .Setup(s => s.UploadFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPath);

        // Force DB commit failure
        _unitOfWorkMock
            .Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Simulated Database Timeout / Deadlock"));

        var request = new UploadDocumentRequestDto
        {
            Title = "Record de Notas",
            StudentRegistration = "2023-0145",
            DocumentType = DocumentType.Transcript,
            FileName = "Record.pdf",
            ContentType = "application/pdf",
            FileStream = stream,
            FileSizeBytes = stream.Length
        };

        // Act
        var result = await _documentService.UploadDocumentAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("PERSISTENCE_TRANSACTION_FAILED", result.ErrorCode);

        // Transaction rollback must have been invoked
        _unitOfWorkMock.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);

        // Compensation: Storage delete must have been called to prevent orphan blob in MinIO!
        _storageServiceMock.Verify(s => s.DeleteFileAsync(expectedPath, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UploadDocumentAsync_DisallowedExtension_ReturnsValidationError()
    {
        // Arrange: Executable file extension
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("echo dangerous script"));
        var request = new UploadDocumentRequestDto
        {
            Title = "Script Malicioso",
            StudentRegistration = "2023-0145",
            DocumentType = DocumentType.Other,
            FileName = "payload.exe",
            ContentType = "application/x-msdownload",
            FileStream = stream,
            FileSizeBytes = stream.Length
        };

        // Act
        var result = await _documentService.UploadDocumentAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_ERROR", result.ErrorCode);
        Assert.Contains(result.Errors, e => e.Contains(".pdf, .png, .jpg y .jpeg"));

        _storageServiceMock.Verify(s => s.UploadFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UploadDocumentAsync_FileSizeExceedsLimit_ReturnsValidationError()
    {
        // Arrange: File size exceeding 50 MB
        using var stream = new MemoryStream(new byte[10]);
        var request = new UploadDocumentRequestDto
        {
            Title = "Documento Gigante",
            StudentRegistration = "2023-0145",
            DocumentType = DocumentType.Other,
            FileName = "large.pdf",
            ContentType = "application/pdf",
            FileStream = stream,
            FileSizeBytes = (50L * 1024 * 1024) + 1 // 50MB + 1 byte
        };

        // Act
        var result = await _documentService.UploadDocumentAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_ERROR", result.ErrorCode);
        Assert.Contains(result.Errors, e => e.Contains("50 MB"));

        _storageServiceMock.Verify(s => s.UploadFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
