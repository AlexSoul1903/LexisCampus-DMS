using LexisCampusDMS.Infraestructure.Shared.Services;
using Xunit;

namespace LexisCampusDMS.UnitTests.Shared;

public class StoragePathBuilderTests
{
    [Fact]
    public void BuildPath_ShouldFollowHierarchicalConvention()
    {
        // Arrange
        var studentRegistration = "2023-0145";
        var fileName = "RecordNotas.pdf";
        var hash = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";
        var year = 2026;

        // Act
        var result = StoragePathBuilder.BuildPath(studentRegistration, fileName, hash, year);

        // Assert: /{matricula}/{anio}/{hash}_{archivo}.pdf
        Assert.Equal("2023-0145/2026/e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855_RecordNotas.pdf", result);
    }

    [Fact]
    public void BuildPath_ShouldSanitizeDangerousCharacters()
    {
        // Arrange
        var studentRegistration = "2023/../0145";
        var fileName = "../../malicious name?.pdf";
        var hash = "abc123hash";
        var year = 2026;

        // Act
        var result = StoragePathBuilder.BuildPath(studentRegistration, fileName, hash, year);

        // Assert
        Assert.DoesNotContain("..", result);
        Assert.DoesNotContain("?", result);
        Assert.StartsWith("2023___0145/2026/abc123hash", result);
    }

    [Theory]
    [InlineData("/2023-0145/2026/file.pdf", "2023-0145/2026/file.pdf")]
    [InlineData(@"2023-0145\2026\file.pdf", "2023-0145/2026/file.pdf")]
    [InlineData("2023-0145/2026/file.pdf", "2023-0145/2026/file.pdf")]
    public void NormalizeKey_ShouldTrimLeadingSlashAndNormalizeSeparators(string input, string expected)
    {
        var result = StoragePathBuilder.NormalizeKey(input);
        Assert.Equal(expected, result);
    }
}
