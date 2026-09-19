using System.Linq.Expressions;
using LexisCampusDMS.Application.DTOs;
using LexisCampusDMS.Application.Interfaces;
using LexisCampusDMS.Application.Options;
using LexisCampusDMS.Application.Services;
using LexisCampusDMS.Core.Domain.Entities;
using LexisCampusDMS.Core.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace LexisCampusDMS.UnitTests;

public class AuthServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IGenericRepository<User, Guid>> _userRepoMock = new();
    private readonly Mock<IGenericRepository<RefreshToken, Guid>> _refreshRepoMock = new();
    private readonly Mock<IPasswordHasherService> _passwordHasherMock = new();
    private readonly Mock<ITokenService> _tokenServiceMock = new();
    private readonly Mock<ILogger<AuthService>> _loggerMock = new();
    private readonly IOptions<JwtOptions> _jwtOptions = Options.Create(new JwtOptions
    {
        Secret = "LexisCampusDMS_Development_SecretKey_AcademicProject_2026_MustBeAtLeast32BytesLong!",
        Issuer = "LexisCampusDMS",
        Audience = "LexisCampusDMS.Clients",
        ExpiryMinutes = 60
    });

    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _unitOfWorkMock.Setup(u => u.Repository<User, Guid>()).Returns(_userRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.Repository<RefreshToken, Guid>()).Returns(_refreshRepoMock.Object);

        _authService = new AuthService(
            _unitOfWorkMock.Object,
            _passwordHasherMock.Object,
            _tokenServiceMock.Object,
            _jwtOptions,
            _loggerMock.Object);
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsSuccessWithTokensAndResetsLockout()
    {
        // Arrange
        var user = new User("admin", "admin@lexiscampus.edu", "hashed_password", "Administrador", "Admin")
        {
            FailedLoginAttempts = 3
        };

        _userRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User> { user });

        _passwordHasherMock.Setup(h => h.VerifyPassword("Admin123!", "hashed_password"))
            .Returns(true);

        _tokenServiceMock.Setup(t => t.GenerateAccessToken(user))
            .Returns("valid.jwt.access_token");

        var refreshToken = new RefreshToken(user.Id, "sample_refresh_token", DateTime.UtcNow.AddDays(7));
        _tokenServiceMock.Setup(t => t.GenerateRefreshToken(user.Id, It.IsAny<string?>()))
            .Returns(refreshToken);

        _refreshRepoMock.Setup(r => r.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(refreshToken);

        var request = new LoginRequestDto { Username = "admin", Password = "Admin123!" };

        // Act
        var result = await _authService.LoginAsync(request, "192.168.1.10");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal("valid.jwt.access_token", result.Data!.AccessToken);
        Assert.Equal("sample_refresh_token", result.Data.RefreshToken);
        Assert.Equal(0, user.FailedLoginAttempts);
        Assert.Null(user.LockoutEndUtc);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_InvalidPassword_IncrementsFailedAttemptsAndReturnsFailure()
    {
        // Arrange
        var user = new User("registro", "registro@lexiscampus.edu", "hashed_pw", "Oficial Registro", "Registro")
        {
            FailedLoginAttempts = 1
        };

        _userRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User> { user });

        _passwordHasherMock.Setup(h => h.VerifyPassword("WrongPassword!", "hashed_pw"))
            .Returns(false);

        var request = new LoginRequestDto { Username = "registro", Password = "WrongPassword!" };

        // Act
        var result = await _authService.LoginAsync(request, "192.168.1.10");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("INVALID_CREDENTIALS", result.ErrorCode);
        Assert.Equal(2, user.FailedLoginAttempts);
        Assert.Null(user.LockoutEndUtc);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_FifthConsecutiveFailedAttempt_LocksOutAccountFor15Minutes()
    {
        // Arrange
        var user = new User("registro", "registro@lexiscampus.edu", "hashed_pw", "Oficial Registro", "Registro")
        {
            FailedLoginAttempts = 4
        };

        _userRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User> { user });

        _passwordHasherMock.Setup(h => h.VerifyPassword("WrongPassword!", "hashed_pw"))
            .Returns(false);

        var request = new LoginRequestDto { Username = "registro", Password = "WrongPassword!" };

        // Act
        var result = await _authService.LoginAsync(request, "192.168.1.10");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("ACCOUNT_LOCKED", result.ErrorCode);
        Assert.Equal(5, user.FailedLoginAttempts);
        Assert.NotNull(user.LockoutEndUtc);
        Assert.True(user.IsLockedOut());
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_WhenAccountIsLockedOut_RejectsImmediatelyWithoutCheckingPassword()
    {
        // Arrange
        var user = new User("locked_user", "locked@lexiscampus.edu", "hashed_pw", "Usuario Bloqueado", "Registro")
        {
            FailedLoginAttempts = 5,
            LockoutEndUtc = DateTime.UtcNow.AddMinutes(10)
        };

        _userRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User> { user });

        var request = new LoginRequestDto { Username = "locked_user", Password = "AnyPassword!" };

        // Act
        var result = await _authService.LoginAsync(request, "192.168.1.10");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("ACCOUNT_LOCKED", result.ErrorCode);
        _passwordHasherMock.Verify(h => h.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_NonExistentUser_ReturnsInvalidCredentials()
    {
        // Arrange
        _userRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User>());

        var request = new LoginRequestDto { Username = "ghost_user", Password = "Password123!" };

        // Act
        var result = await _authService.LoginAsync(request, "192.168.1.10");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("INVALID_CREDENTIALS", result.ErrorCode);
    }

    [Fact]
    public async Task RefreshTokenAsync_ValidToken_RotatesTokenAndReturnsNewTokens()
    {
        // Arrange
        var user = new User("admin", "admin@lexiscampus.edu", "hash", "Admin User", "Admin");
        var oldRefreshToken = new RefreshToken(user.Id, "old_token_123", DateTime.UtcNow.AddDays(2));

        _refreshRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<RefreshToken, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RefreshToken> { oldRefreshToken });

        _userRepoMock.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var newRefreshToken = new RefreshToken(user.Id, "new_rotated_token_456", DateTime.UtcNow.AddDays(7));
        _tokenServiceMock.Setup(t => t.GenerateRefreshToken(user.Id, It.IsAny<string?>()))
            .Returns(newRefreshToken);

        _tokenServiceMock.Setup(t => t.GenerateAccessToken(user))
            .Returns("new.jwt.access_token");

        _refreshRepoMock.Setup(r => r.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(newRefreshToken);

        var request = new RefreshTokenRequestDto { RefreshToken = "old_token_123" };

        // Act
        var result = await _authService.RefreshTokenAsync(request, "192.168.1.10");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal("new.jwt.access_token", result.Data!.AccessToken);
        Assert.Equal("new_rotated_token_456", result.Data.RefreshToken);
        Assert.True(oldRefreshToken.IsRevoked);
        Assert.Equal("new_rotated_token_456", oldRefreshToken.ReplacedByToken);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RefreshTokenAsync_RevokedToken_ReturnsRevokedError()
    {
        // Arrange
        var oldRefreshToken = new RefreshToken(Guid.NewGuid(), "revoked_token", DateTime.UtcNow.AddDays(2));
        oldRefreshToken.Revoke();

        _refreshRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<RefreshToken, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RefreshToken> { oldRefreshToken });

        var request = new RefreshTokenRequestDto { RefreshToken = "revoked_token" };

        // Act
        var result = await _authService.RefreshTokenAsync(request, "192.168.1.10");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("REVOKED_REFRESH_TOKEN", result.ErrorCode);
    }

    [Fact]
    public async Task RefreshTokenAsync_ExpiredToken_ReturnsExpiredError()
    {
        // Arrange
        var expiredToken = new RefreshToken(Guid.NewGuid(), "expired_token", DateTime.UtcNow.AddHours(-1));

        _refreshRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<RefreshToken, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RefreshToken> { expiredToken });

        var request = new RefreshTokenRequestDto { RefreshToken = "expired_token" };

        // Act
        var result = await _authService.RefreshTokenAsync(request, "192.168.1.10");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("EXPIRED_REFRESH_TOKEN", result.ErrorCode);
    }
}
