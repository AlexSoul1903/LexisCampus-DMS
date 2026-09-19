using LexisCampusDMS.Infraestructure.Shared.Services;
using Xunit;

namespace LexisCampusDMS.UnitTests;

public class PasswordHasherServiceTests
{
    private readonly BcryptPasswordHasherService _hasher = new();

    [Fact]
    public void HashPassword_ValidInput_ReturnsBcryptHash()
    {
        // Act
        var hash = _hasher.HashPassword("Admin123!");

        // Assert
        Assert.NotNull(hash);
        Assert.StartsWith("$2a$11$", hash);
    }

    [Fact]
    public void VerifyPassword_MatchingPassword_ReturnsTrue()
    {
        // Arrange
        var password = "SecurePassword2026!";
        var hash = _hasher.HashPassword(password);

        // Act
        var result = _hasher.VerifyPassword(password, hash);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void VerifyPassword_IncorrectPassword_ReturnsFalse()
    {
        // Arrange
        var password = "SecurePassword2026!";
        var hash = _hasher.HashPassword(password);

        // Act
        var result = _hasher.VerifyPassword("WrongPassword123!", hash);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("", "some_hash")]
    [InlineData("password", "")]
    [InlineData(null, null)]
    public void VerifyPassword_NullOrEmpty_ReturnsFalse(string? password, string? hash)
    {
        // Act
        var result = _hasher.VerifyPassword(password!, hash!);

        // Assert
        Assert.False(result);
    }
}
