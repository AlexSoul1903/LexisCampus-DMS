using LexisCampusDMS.Core.Domain.Common;

namespace LexisCampusDMS.Core.Domain.Entities;

public class User : AuditableEntity<Guid>
{
    public const int MaxFailedAccessAttempts = 5;
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? Department { get; set; }
    public string? StudentRegistration { get; set; }
    public int FailedLoginAttempts { get; set; } = 0;
    public DateTime? LockoutEndUtc { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation property
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    public User()
    {
        Id = Guid.NewGuid();
    }

    public User(
        string username,
        string email,
        string passwordHash,
        string fullName,
        string role,
        string? department = null,
        string? studentRegistration = null) : this()
    {
        Username = username;
        Email = email;
        PasswordHash = passwordHash;
        FullName = fullName;
        Role = role;
        Department = department;
        StudentRegistration = studentRegistration;
        CreatedAtUtc = DateTime.UtcNow;
        CreatedBy = "System";
    }

    public bool IsLockedOut()
    {
        return LockoutEndUtc.HasValue && LockoutEndUtc.Value > DateTime.UtcNow;
    }

    public void RecordFailedLogin()
    {
        FailedLoginAttempts++;

        if (FailedLoginAttempts >= MaxFailedAccessAttempts)
        {
            LockoutEndUtc = DateTime.UtcNow.Add(LockoutDuration);
        }
    }

    public void ResetLockout()
    {
        FailedLoginAttempts = 0;
        LockoutEndUtc = null;
    }
}
