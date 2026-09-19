using LexisCampusDMS.Application.Common;
using LexisCampusDMS.Application.DTOs;
using LexisCampusDMS.Application.Interfaces;
using LexisCampusDMS.Server.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LexisCampusDMS.UnitTests;

public class AuthControllerTests
{
    private readonly Mock<IAuthService> _authServiceMock = new();
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly Mock<ILogger<AuthController>> _loggerMock = new();
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _currentUserServiceMock.Setup(c => c.IpAddress).Returns("127.0.0.1");

        _controller = new AuthController(
            _authServiceMock.Object,
            _currentUserServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Login_ValidRequest_ReturnsOkWithTokens()
    {
        // Arrange
        var request = new LoginRequestDto { Username = "admin", Password = "Admin123!" };
        var authResponse = new AuthResponseDto
        {
            AccessToken = "valid_access_token",
            RefreshToken = "valid_refresh_token",
            ExpiresInSeconds = 3600,
            User = new UserInfoDto { Username = "admin", Role = "Admin" }
        };

        _authServiceMock
            .Setup(s => s.LoginAsync(request, "127.0.0.1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AuthResponseDto>.Success(authResponse));

        // Act
        var actionResult = await _controller.Login(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var result = Assert.IsType<Result<AuthResponseDto>>(okResult.Value);
        Assert.True(result.IsSuccess);
        Assert.Equal("valid_access_token", result.Data!.AccessToken);
    }

    [Fact]
    public async Task Login_InvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var request = new LoginRequestDto { Username = "admin", Password = "WrongPassword!" };

        _authServiceMock
            .Setup(s => s.LoginAsync(request, "127.0.0.1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AuthResponseDto>.Failure("Credenciales inválidas.", "INVALID_CREDENTIALS"));

        // Act
        var actionResult = await _controller.Login(request);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(actionResult);
        var result = Assert.IsType<Result<AuthResponseDto>>(unauthorizedResult.Value);
        Assert.False(result.IsSuccess);
        Assert.Equal("INVALID_CREDENTIALS", result.ErrorCode);
    }

    [Fact]
    public async Task Login_AccountLocked_ReturnsUnauthorized()
    {
        // Arrange
        var request = new LoginRequestDto { Username = "admin", Password = "WrongPassword!" };

        _authServiceMock
            .Setup(s => s.LoginAsync(request, "127.0.0.1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AuthResponseDto>.Failure("Cuenta bloqueada temporalmente.", "ACCOUNT_LOCKED"));

        // Act
        var actionResult = await _controller.Login(request);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(actionResult);
        var result = Assert.IsType<Result<AuthResponseDto>>(unauthorizedResult.Value);
        Assert.False(result.IsSuccess);
        Assert.Equal("ACCOUNT_LOCKED", result.ErrorCode);
    }

    [Fact]
    public async Task RefreshToken_ValidToken_ReturnsOk()
    {
        // Arrange
        var request = new RefreshTokenRequestDto { RefreshToken = "valid_refresh_token" };
        var authResponse = new AuthResponseDto
        {
            AccessToken = "new_access_token",
            RefreshToken = "new_refresh_token",
            ExpiresInSeconds = 3600
        };

        _authServiceMock
            .Setup(s => s.RefreshTokenAsync(request, "127.0.0.1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AuthResponseDto>.Success(authResponse));

        // Act
        var actionResult = await _controller.RefreshToken(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var result = Assert.IsType<Result<AuthResponseDto>>(okResult.Value);
        Assert.True(result.IsSuccess);
        Assert.Equal("new_access_token", result.Data!.AccessToken);
    }

    [Fact]
    public async Task RefreshToken_InvalidToken_ReturnsUnauthorized()
    {
        // Arrange
        var request = new RefreshTokenRequestDto { RefreshToken = "invalid_token" };

        _authServiceMock
            .Setup(s => s.RefreshTokenAsync(request, "127.0.0.1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AuthResponseDto>.Failure("Token inválido.", "INVALID_REFRESH_TOKEN"));

        // Act
        var actionResult = await _controller.RefreshToken(request);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(actionResult);
        var result = Assert.IsType<Result<AuthResponseDto>>(unauthorizedResult.Value);
        Assert.False(result.IsSuccess);
        Assert.Equal("INVALID_REFRESH_TOKEN", result.ErrorCode);
    }
}
