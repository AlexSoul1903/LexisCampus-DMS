using LexisCampusDMS.Application.Interfaces;
using LexisCampusDMS.Core.Domain.Entities;
using LexisCampusDMS.Core.Domain.Enums;
using LexisCampusDMS.Core.Domain.Interfaces;
using LexisCampusDMS.Server.Middlewares;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LexisCampusDMS.UnitTests;

public class AuditLogMiddlewareTests
{
    private readonly Mock<ILogger<AuditLogMiddleware>> _loggerMock = new();
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IGenericRepository<AuditLog, Guid>> _auditRepoMock = new();

    public AuditLogMiddlewareTests()
    {
        _unitOfWorkMock.Setup(u => u.Repository<AuditLog, Guid>()).Returns(_auditRepoMock.Object);
        _currentUserServiceMock.Setup(c => c.UserId).Returns("user-456");
        _currentUserServiceMock.Setup(c => c.IpAddress).Returns("192.168.1.100");
    }

    [Fact]
    public async Task InvokeAsync_WhenGetDocumentSucceeds_RecordsViewedAuditLog()
    {
        // Arrange
        var docId = Guid.NewGuid();
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = $"/api/documents/{docId}";
        context.Request.RouteValues = new RouteValueDictionary { { "id", docId.ToString() } };
        context.Response.StatusCode = StatusCodes.Status200OK;

        AuditLog? capturedLog = null;
        _auditRepoMock
            .Setup(r => r.AddAsync(It.IsAny<AuditLog>(), It.IsAny<CancellationToken>()))
            .Callback<AuditLog, CancellationToken>((log, _) => capturedLog = log)
            .ReturnsAsync((AuditLog log, CancellationToken _) => log);

        RequestDelegate next = (ctx) => Task.CompletedTask;
        var middleware = new AuditLogMiddleware(next, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context, _currentUserServiceMock.Object, _unitOfWorkMock.Object);

        // Assert
        Assert.NotNull(capturedLog);
        Assert.Equal("user-456", capturedLog!.UserId);
        Assert.Equal(AuditAction.Viewed, capturedLog.Action);
        Assert.Equal(docId, capturedLog.DocumentId);
        Assert.Equal("192.168.1.100", capturedLog.IpAddress);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WhenGetDocumentDownloadSucceeds_RecordsDownloadedAuditLog()
    {
        // Arrange
        var docId = Guid.NewGuid();
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = $"/api/documents/{docId}/download";
        context.Request.RouteValues = new RouteValueDictionary { { "id", docId.ToString() } };
        context.Response.StatusCode = StatusCodes.Status200OK;

        AuditLog? capturedLog = null;
        _auditRepoMock
            .Setup(r => r.AddAsync(It.IsAny<AuditLog>(), It.IsAny<CancellationToken>()))
            .Callback<AuditLog, CancellationToken>((log, _) => capturedLog = log)
            .ReturnsAsync((AuditLog log, CancellationToken _) => log);

        RequestDelegate next = (ctx) => Task.CompletedTask;
        var middleware = new AuditLogMiddleware(next, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context, _currentUserServiceMock.Object, _unitOfWorkMock.Object);

        // Assert
        Assert.NotNull(capturedLog);
        Assert.Equal(AuditAction.Downloaded, capturedLog!.Action);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WhenResponseIsError_DoesNotRecordAuditLog()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/api/documents/non-existent";
        context.Response.StatusCode = StatusCodes.Status404NotFound;

        RequestDelegate next = (ctx) => Task.CompletedTask;
        var middleware = new AuditLogMiddleware(next, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context, _currentUserServiceMock.Object, _unitOfWorkMock.Object);

        // Assert
        _auditRepoMock.Verify(r => r.AddAsync(It.IsAny<AuditLog>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_WhenNonDocumentEndpoint_DoesNotRecordAuditLog()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/api/auth/profile";
        context.Response.StatusCode = StatusCodes.Status200OK;

        RequestDelegate next = (ctx) => Task.CompletedTask;
        var middleware = new AuditLogMiddleware(next, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context, _currentUserServiceMock.Object, _unitOfWorkMock.Object);

        // Assert
        _auditRepoMock.Verify(r => r.AddAsync(It.IsAny<AuditLog>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
