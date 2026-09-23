using System.Linq.Expressions;
using FluentValidation;
using LexisCampusDMS.Application.DTOs;
using LexisCampusDMS.Application.Interfaces;
using LexisCampusDMS.Application.Services;
using LexisCampusDMS.Application.Validators;
using LexisCampusDMS.Core.Domain.Entities;
using LexisCampusDMS.Core.Domain.Enums;
using LexisCampusDMS.Core.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LexisCampusDMS.UnitTests;

public class UserServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IGenericRepository<User, Guid>> _userGenericRepoMock = new();
    private readonly Mock<IGenericRepository<AuditLog, Guid>> _auditRepoMock = new();
    private readonly Mock<IPasswordHasherService> _passwordHasherMock = new();
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly Mock<ILogger<UserService>> _loggerMock = new();

    private readonly IValidator<CreateUserRequestDto> _createValidator = new CreateUserRequestDtoValidator();
    private readonly IValidator<UpdateUserRequestDto> _updateValidator = new UpdateUserRequestDtoValidator();
    private readonly IValidator<ChangeUserRoleRequestDto> _roleValidator = new ChangeUserRoleRequestDtoValidator();

    private readonly UserService _userService;

    public UserServiceTests()
    {
        _unitOfWorkMock.Setup(u => u.Repository<User, Guid>()).Returns(_userGenericRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.Repository<AuditLog, Guid>()).Returns(_auditRepoMock.Object);

        _currentUserServiceMock.Setup(c => c.UserId).Returns("admin_guid");
        _currentUserServiceMock.Setup(c => c.UserName).Returns("admin_user");
        _currentUserServiceMock.Setup(c => c.IpAddress).Returns("127.0.0.1");

        _userService = new UserService(
            _unitOfWorkMock.Object,
            _userRepositoryMock.Object,
            _passwordHasherMock.Object,
            _currentUserServiceMock.Object,
            _createValidator,
            _updateValidator,
            _roleValidator,
            _loggerMock.Object);
    }

    [Fact]
    public async Task CreateUserAsync_ValidRequest_HashesPasswordAndPersistsAudit()
    {
        // Arrange
        var request = new CreateUserRequestDto
        {
            Username = "cristal",
            Email = "cristal@lexiscampus.edu",
            Password = "Password123!",
            FullName = "Cristal",
            Role = "Registro",
            Department = "Admisiones"
        };

        _userGenericRepoMock.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _passwordHasherMock.Setup(h => h.HashPassword("Password123!"))
            .Returns("hashed_pwd_123");

        User? capturedUser = null;
        _userGenericRepoMock.Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((u, _) => capturedUser = u)
            .ReturnsAsync((User u, CancellationToken _) => u);

        AuditLog? capturedAudit = null;
        _auditRepoMock.Setup(r => r.AddAsync(It.IsAny<AuditLog>(), It.IsAny<CancellationToken>()))
            .Callback<AuditLog, CancellationToken>((a, _) => capturedAudit = a)
            .ReturnsAsync((AuditLog a, CancellationToken _) => a);

        // Act
        var result = await _userService.CreateUserAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal("cristal", result.Data!.Username);
        Assert.Equal("Cristal", result.Data.FullName);
        Assert.Equal("Registro", result.Data.Role);
        Assert.Equal("hashed_pwd_123", capturedUser!.PasswordHash);

        Assert.NotNull(capturedAudit);
        Assert.Equal(AuditAction.Created, capturedAudit!.Action);
        Assert.Contains("cristal", capturedAudit.Details);

        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateUserAsync_DuplicateUsername_ReturnsFailure()
    {
        // Arrange
        var request = new CreateUserRequestDto
        {
            Username = "cristal",
            Email = "cristal_nueva@lexiscampus.edu",
            Password = "Password123!",
            FullName = "Cristal",
            Role = "Registro"
        };

        // Username exists
        _userGenericRepoMock.Setup(r => r.ExistsAsync(It.Is<Expression<Func<User, bool>>>(e => e.ToString().Contains("Username")), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _userService.CreateUserAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("USERNAME_ALREADY_EXISTS", result.ErrorCode);
        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateUserAsync_DuplicateEmail_ReturnsFailure()
    {
        // Arrange
        var request = new CreateUserRequestDto
        {
            Username = "alexa",
            Email = "alexa@lexiscampus.edu",
            Password = "Password123!",
            FullName = "Alexa",
            Role = "Registro"
        };

        // Username does not exist, email exists
        _userGenericRepoMock.SetupSequence(r => r.ExistsAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false)
            .ReturnsAsync(true);

        // Act
        var result = await _userService.CreateUserAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("EMAIL_ALREADY_EXISTS", result.ErrorCode);
        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateUserAsync_InvalidRole_ReturnsValidationFailure()
    {
        // Arrange
        var request = new CreateUserRequestDto
        {
            Username = "alexa",
            Email = "alexa@lexiscampus.edu",
            Password = "Password123!",
            FullName = "Alexa",
            Role = "SuperAdmin" // Invalid role
        };

        // Act
        var result = await _userService.CreateUserAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_FAILED", result.ErrorCode);
    }

    [Fact]
    public async Task GetUsersAsync_ReturnsPagedResults()
    {
        // Arrange
        var filter = new UserFilterDto
        {
            SearchTerm = "cristal",
            Role = "Registro",
            IsActive = true,
            PageNumber = 1,
            PageSize = 10
        };

        var users = new List<User>
        {
            new("cristal", "cristal@lexiscampus.edu", "hash", "Cristal", "Registro"),
            new("alexa", "alexa@lexiscampus.edu", "hash", "Alexa", "Admin")
        };

        _userRepositoryMock.Setup(r => r.SearchUsersAsync("cristal", "Registro", true, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((users, 2));

        // Act
        var result = await _userService.GetUsersAsync(filter);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Data!.Count);
        Assert.Equal("cristal", result.Data[0].Username);
        Assert.Equal("Cristal", result.Data[0].FullName);
        Assert.Equal("alexa", result.Data[1].Username);
        Assert.Equal("Alexa", result.Data[1].FullName);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingUser_ReturnsSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User("cristal", "cristal@lexiscampus.edu", "hash", "Cristal", "Registro")
        {
            Id = userId
        };

        _userGenericRepoMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _userService.GetByIdAsync(userId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal("cristal", result.Data!.Username);
        Assert.Equal("Cristal", result.Data.FullName);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentUser_ReturnsNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userGenericRepoMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _userService.GetByIdAsync(userId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("USER_NOT_FOUND", result.ErrorCode);
    }

    [Fact]
    public async Task UpdateUserAsync_ValidRequest_UpdatesProfileAndLogsAudit()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User("cristal", "cristal@lexiscampus.edu", "hash", "Cristal", "Registro")
        {
            Id = userId
        };

        _userGenericRepoMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var request = new UpdateUserRequestDto
        {
            FullName = "Cristal Actualizada",
            Department = "Auditoria Externa",
            StudentRegistration = "2026-9999"
        };

        AuditLog? capturedAudit = null;
        _auditRepoMock.Setup(r => r.AddAsync(It.IsAny<AuditLog>(), It.IsAny<CancellationToken>()))
            .Callback<AuditLog, CancellationToken>((a, _) => capturedAudit = a)
            .ReturnsAsync((AuditLog a, CancellationToken _) => a);

        // Act
        var result = await _userService.UpdateUserAsync(userId, request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Cristal Actualizada", user.FullName);
        Assert.Equal("Auditoria Externa", user.Department);
        Assert.Equal("2026-9999", user.StudentRegistration);

        Assert.NotNull(capturedAudit);
        Assert.Equal(AuditAction.Updated, capturedAudit!.Action);
        Assert.Contains("cristal", capturedAudit.Details);

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ChangeUserRoleAsync_ValidRole_UpdatesRoleAndLogsAudit()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User("alexa", "alexa@lexiscampus.edu", "hash", "Alexa", "Registro")
        {
            Id = userId
        };

        _userGenericRepoMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var request = new ChangeUserRoleRequestDto { Role = "Auditor" };

        AuditLog? capturedAudit = null;
        _auditRepoMock.Setup(r => r.AddAsync(It.IsAny<AuditLog>(), It.IsAny<CancellationToken>()))
            .Callback<AuditLog, CancellationToken>((a, _) => capturedAudit = a)
            .ReturnsAsync((AuditLog a, CancellationToken _) => a);

        // Act
        var result = await _userService.ChangeUserRoleAsync(userId, request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Auditor", user.Role);

        Assert.NotNull(capturedAudit);
        Assert.Equal(AuditAction.Updated, capturedAudit!.Action);
        Assert.Contains("Auditor", capturedAudit.Details);
        Assert.Contains("alexa", capturedAudit.Details);

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ChangeUserStatusAsync_DeactivateAndResetLockout_UpdatesStatusAndResetsLockout()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User("cristal", "cristal@lexiscampus.edu", "hash", "Cristal", "Registro")
        {
            Id = userId,
            IsActive = true,
            FailedLoginAttempts = 5,
            LockoutEndUtc = DateTime.UtcNow.AddMinutes(15)
        };

        _userGenericRepoMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var request = new ChangeUserStatusRequestDto
        {
            IsActive = false,
            ResetLockout = true
        };

        AuditLog? capturedAudit = null;
        _auditRepoMock.Setup(r => r.AddAsync(It.IsAny<AuditLog>(), It.IsAny<CancellationToken>()))
            .Callback<AuditLog, CancellationToken>((a, _) => capturedAudit = a)
            .ReturnsAsync((AuditLog a, CancellationToken _) => a);

        // Act
        var result = await _userService.ChangeUserStatusAsync(userId, request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(user.IsActive);
        Assert.Equal(0, user.FailedLoginAttempts);
        Assert.Null(user.LockoutEndUtc);

        Assert.NotNull(capturedAudit);
        Assert.Equal(AuditAction.StatusChanged, capturedAudit!.Action);
        Assert.Contains("cristal", capturedAudit.Details);

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ChangeUserStatusAsync_WhenDeactivatingRootAdmin_ReturnsCannotDeactivateRootAdmin()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var rootAdmin = new User("admin", "admin@lexiscampus.edu", "hash", "Admin", "Admin")
        {
            Id = userId,
            IsActive = true
        };

        _userGenericRepoMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rootAdmin);

        var request = new ChangeUserStatusRequestDto { IsActive = false };

        // Act
        var result = await _userService.ChangeUserStatusAsync(userId, request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("CANNOT_DEACTIVATE_ROOT_ADMIN", result.ErrorCode);
        Assert.True(rootAdmin.IsActive);
    }

    [Fact]
    public async Task ChangeUserStatusAsync_WhenDeactivatingSelf_ReturnsCannotDeactivateSelf()
    {
        // Arrange
        var selfUserId = Guid.NewGuid();
        var selfUser = new User("other_admin", "other_admin@lexiscampus.edu", "hash", "Other Admin", "Admin")
        {
            Id = selfUserId,
            IsActive = true
        };

        _currentUserServiceMock.Setup(c => c.UserId).Returns(selfUserId.ToString());
        _currentUserServiceMock.Setup(c => c.UserName).Returns("other_admin");

        _userGenericRepoMock.Setup(r => r.GetByIdAsync(selfUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(selfUser);

        var request = new ChangeUserStatusRequestDto { IsActive = false };

        // Act
        var result = await _userService.ChangeUserStatusAsync(selfUserId, request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("CANNOT_DEACTIVATE_SELF", result.ErrorCode);
        Assert.True(selfUser.IsActive);
    }

    [Fact]
    public async Task ChangeUserStatusAsync_WhenDeactivatingLastActiveAdmin_ReturnsLastActiveAdminError()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var lastAdmin = new User("secondary_admin", "sec@lexiscampus.edu", "hash", "Sec Admin", "Admin")
        {
            Id = adminId,
            IsActive = true
        };

        _currentUserServiceMock.Setup(c => c.UserId).Returns(Guid.NewGuid().ToString());
        _currentUserServiceMock.Setup(c => c.UserName).Returns("some_operator");

        _userGenericRepoMock.Setup(r => r.GetByIdAsync(adminId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lastAdmin);

        _userGenericRepoMock.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false); // No other active admin

        var request = new ChangeUserStatusRequestDto { IsActive = false };

        // Act
        var result = await _userService.ChangeUserStatusAsync(adminId, request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("LAST_ACTIVE_ADMIN", result.ErrorCode);
        Assert.True(lastAdmin.IsActive);
    }

    [Fact]
    public async Task ChangeUserRoleAsync_WhenDemotingRootAdmin_ReturnsCannotDemoteRootAdmin()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var rootAdmin = new User("admin", "admin@lexiscampus.edu", "hash", "Admin", "Admin")
        {
            Id = userId
        };

        _userGenericRepoMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rootAdmin);

        var request = new ChangeUserRoleRequestDto { Role = "Registro" };

        // Act
        var result = await _userService.ChangeUserRoleAsync(userId, request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("CANNOT_DEMOTE_ROOT_ADMIN", result.ErrorCode);
        Assert.Equal("Admin", rootAdmin.Role);
    }

    [Fact]
    public async Task ChangeUserRoleAsync_WhenDemotingLastActiveAdmin_ReturnsLastActiveAdminError()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var lastAdmin = new User("admin2", "admin2@lexiscampus.edu", "hash", "Admin Two", "Admin")
        {
            Id = adminId
        };

        _currentUserServiceMock.Setup(c => c.UserId).Returns(Guid.NewGuid().ToString());
        _currentUserServiceMock.Setup(c => c.UserName).Returns("operator");

        _userGenericRepoMock.Setup(r => r.GetByIdAsync(adminId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lastAdmin);

        _userGenericRepoMock.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false); // No other active admin

        var request = new ChangeUserRoleRequestDto { Role = "Auditor" };

        // Act
        var result = await _userService.ChangeUserRoleAsync(adminId, request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("LAST_ACTIVE_ADMIN", result.ErrorCode);
        Assert.Equal("Admin", lastAdmin.Role);
    }
}
