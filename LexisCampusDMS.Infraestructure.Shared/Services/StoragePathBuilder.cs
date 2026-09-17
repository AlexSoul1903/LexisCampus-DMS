using System.Text.RegularExpressions;

namespace LexisCampusDMS.Infraestructure.Shared.Services;

public static class StoragePathBuilder
{
    private static readonly Regex InvalidCharRegex = new(@"[^a-zA-Z0-9_\-\.]", RegexOptions.Compiled);

    /// <summary>
    /// Builds a hierarchical and immutable storage path adhering to the convention:
    /// {studentRegistration}/{year}/{fileHashSha256}_{sanitizedFileName}
    /// </summary>
    public static string BuildPath(
        string studentRegistration, 
        string fileName, 
        string fileHashSha256, 
        int? year = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(studentRegistration, nameof(studentRegistration));
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName, nameof(fileName));
        ArgumentException.ThrowIfNullOrWhiteSpace(fileHashSha256, nameof(fileHashSha256));

        var targetYear = year ?? DateTime.UtcNow.Year;
        var sanitizedRegistration = Sanitize(studentRegistration);
        var sanitizedFileName = Sanitize(Path.GetFileName(fileName));
        var sanitizedHash = Sanitize(fileHashSha256);

        // Standard S3 object key hierarchy without leading slash
        return $"{sanitizedRegistration}/{targetYear}/{sanitizedHash}_{sanitizedFileName}";
    }

    /// <summary>
    /// Normalizes path keys to ensure consistent format across S3 operations.
    /// </summary>
    public static string NormalizeKey(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return string.Empty;

        return path.TrimStart('/').Replace('\\', '/');
    }

    private static string Sanitize(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var withoutTraversal = input.Trim().Replace("..", "_");
        var cleaned = InvalidCharRegex.Replace(withoutTraversal, "_");
        return cleaned.Length > 100 ? cleaned[..100] : cleaned;
    }
}
