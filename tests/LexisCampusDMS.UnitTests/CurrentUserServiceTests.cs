using System.Net;
using System.Security.Claims;
using LexisCampusDMS.Server.Services;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace LexisCampusDMS.UnitTests;

public class CurrentUserServiceTests
{
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;

    public CurrentUserServiceTests()
    {
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
    }

    [Fact]
    public void Properties_WhenHttpContextIsNull_ReturnsDefaults()
    {
        // Arrange
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);
        var service = new CurrentUserService(_httpContextAccessorMock.Object);

        // Act & Assert
        Assert.Null(service.UserId);
        Assert.Null(service.UserName);
        Assert.Empty(service.Roles);
        Assert.Null(service.IpAddress);
        Assert.False(service.IsAuthenticated);
    }

    [Fact]
    public void Properties_WhenUserIsAuthenticatedWithClaims_ReturnsExtractedValues()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "user-123"),
            new(ClaimTypes.Name, "alex.user"),
            new(ClaimTypes.Role, "Registro"),
            new(ClaimTypes.Role, "Admin")
        };
        var identity = new ClaimsIdentity(claims, "Bearer");
        context.User = new ClaimsPrincipal(identity);
        context.Connection.RemoteIpAddress = IPAddress.Parse("192.168.1.50");

        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(context);
        var service = new CurrentUserService(_httpContextAccessorMock.Object);

        // Act & Assert
        Assert.Equal("user-123", service.UserId);
        Assert.Equal("alex.user", service.UserName);
        Assert.True(service.IsAuthenticated);
        Assert.Contains("Registro", service.Roles);
        Assert.Contains("Admin", service.Roles);
        Assert.Equal("192.168.1.50", service.IpAddress);
    }

    [Fact]
    public void IpAddress_WhenXForwardedForHeaderPresent_ExtractsFirstIp()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Forwarded-For"] = "203.0.113.195, 70.41.3.18, 150.172.238.178";
        context.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.1");

        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(context);
        var service = new CurrentUserService(_httpContextAccessorMock.Object);

        // Act & Assert
        Assert.Equal("203.0.113.195", service.IpAddress);
    }
}
