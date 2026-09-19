using LexisCampusDMS.Application.Common;
using LexisCampusDMS.Application.DTOs;
using LexisCampusDMS.Application.Interfaces;
using LexisCampusDMS.Server.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace LexisCampusDMS.UnitTests.Server;

public class PublicVerificationControllerTests
{
    private readonly Mock<IDocumentService> _documentServiceMock = new();
    private readonly IMemoryCache _memoryCache = new MemoryCache(new MemoryCacheOptions());
    private readonly PublicVerificationController _controller;

    public PublicVerificationControllerTests()
    {
        _controller = new PublicVerificationController(
            _documentServiceMock.Object,
            _memoryCache,
            NullLogger<PublicVerificationController>.Instance);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    [Fact]
    public async Task Verify_CacheMiss_CallsServiceSavesToMemoryCacheAndReturnsOk()
    {
        // Arrange
        var testHash = "hash1234567890abcdef";
        var expectedDto = new PublicVerificationResponseDto
        {
            IsValid = true,
            Title = "Certificado de Calificaciones",
            AnonymizedStudentRegistration = "2023-****",
            SigningDean = "Dra. Carmen Valenzuela",
            InstitutionalSeal = "SELLO-CERT-LEXISCAMPUS-HASH12345678-20260919"
        };

        _documentServiceMock
            .Setup(s => s.VerifyPublicDocumentAsync(testHash, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PublicVerificationResponseDto>.Success(expectedDto));

        // Act
        var result = await _controller.Verify(testHash, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        Assert.Equal("MISS", _controller.Response.Headers["X-Cache"]);

        // Second call should hit the cache without calling service again
        var secondResult = await _controller.Verify(testHash, CancellationToken.None);
        var secondOkResult = Assert.IsType<OkObjectResult>(secondResult);
        Assert.Equal("HIT", _controller.Response.Headers["X-Cache"]);

        _documentServiceMock.Verify(
            s => s.VerifyPublicDocumentAsync(testHash, It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Fact]
    public async Task Verify_DocumentNotFound_ReturnsNotFound()
    {
        // Arrange
        var nonExistentHash = "not_found_hash";
        _documentServiceMock
            .Setup(s => s.VerifyPublicDocumentAsync(nonExistentHash, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PublicVerificationResponseDto>.Failure("No se encontró el documento", "DOCUMENT_NOT_FOUND"));

        // Act
        var result = await _controller.Verify(nonExistentHash, CancellationToken.None);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFoundResult.Value);
    }

    [Fact]
    public async Task Verify_EmptyHash_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.Verify("   ", CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequestResult.Value);
    }
}
