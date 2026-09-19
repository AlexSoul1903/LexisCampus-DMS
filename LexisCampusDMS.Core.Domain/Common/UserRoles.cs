namespace LexisCampusDMS.Core.Domain.Common;

public static class UserRoles
{
    public const string Admin = "Admin";
    public const string Registro = "Registro";
    public const string Auditor = "Auditor";

    public static readonly IReadOnlyList<string> All = new[] { Admin, Registro, Auditor };

    public static bool IsValid(string? role) =>
        !string.IsNullOrWhiteSpace(role) &&
        All.Any(r => string.Equals(r, role.Trim(), StringComparison.OrdinalIgnoreCase));

    public static string Normalize(string role) =>
        All.First(r => string.Equals(r, role.Trim(), StringComparison.OrdinalIgnoreCase));
}
