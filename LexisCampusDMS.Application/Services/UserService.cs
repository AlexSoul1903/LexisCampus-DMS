using FluentValidation;
using LexisCampusDMS.Application.Common;
using LexisCampusDMS.Application.DTOs;
using LexisCampusDMS.Application.Interfaces;
using LexisCampusDMS.Core.Domain.Common;
using LexisCampusDMS.Core.Domain.Entities;
using LexisCampusDMS.Core.Domain.Enums;
using LexisCampusDMS.Core.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace LexisCampusDMS.Application.Services;

public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasherService _passwordHasher;
    private readonly ICurrentUserService _currentUserService;
    private readonly IValidator<CreateUserRequestDto> _createValidator;
    private readonly IValidator<UpdateUserRequestDto> _updateValidator;
    private readonly IValidator<ChangeUserRoleRequestDto> _roleValidator;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IUnitOfWork unitOfWork,
        IUserRepository userRepository,
        IPasswordHasherService passwordHasher,
        ICurrentUserService currentUserService,
        IValidator<CreateUserRequestDto> createValidator,
        IValidator<UpdateUserRequestDto> updateValidator,
        IValidator<ChangeUserRoleRequestDto> roleValidator,
        ILogger<UserService> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        _createValidator = createValidator ?? throw new ArgumentNullException(nameof(createValidator));
        _updateValidator = updateValidator ?? throw new ArgumentNullException(nameof(updateValidator));
        _roleValidator = roleValidator ?? throw new ArgumentNullException(nameof(roleValidator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<UserResponseDto>> CreateUserAsync(
        CreateUserRequestDto request, 
        CancellationToken cancellationToken = default)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            var firstError = validation.Errors[0].ErrorMessage;
            return Result<UserResponseDto>.Failure(
                firstError,
                "VALIDATION_FAILED",
                validation.Errors.Select(e => e.ErrorMessage));
        }

        var normalizedUsername = request.Username.Trim();
        var normalizedEmail = request.Email.Trim();

        var usernameExists = await _unitOfWork.Repository<User, Guid>().ExistsAsync(
            u => u.Username.ToLower() == normalizedUsername.ToLower() && !u.IsDeleted,
            cancellationToken);

        if (usernameExists)
        {
            _logger.LogWarning("CreateUserAsync failed: username '{Username}' already exists", normalizedUsername);
            return Result<UserResponseDto>.Failure(
                "El nombre de usuario ya está registrado en el sistema.",
                "USERNAME_ALREADY_EXISTS");
        }

        var emailExists = await _unitOfWork.Repository<User, Guid>().ExistsAsync(
            u => u.Email.ToLower() == normalizedEmail.ToLower() && !u.IsDeleted,
            cancellationToken);

        if (emailExists)
        {
            _logger.LogWarning("CreateUserAsync failed: email '{Email}' already exists", normalizedEmail);
            return Result<UserResponseDto>.Failure(
                "El correo electrónico ya está registrado en el sistema.",
                "EMAIL_ALREADY_EXISTS");
        }

        var normalizedRole = UserRoles.Normalize(request.Role);
        var passwordHash = _passwordHasher.HashPassword(request.Password);
        var actor = _currentUserService.UserName ?? "Admin";

        var user = new User(
            username: normalizedUsername,
            email: normalizedEmail,
            passwordHash: passwordHash,
            fullName: request.FullName.Trim(),
            role: normalizedRole,
            department: request.Department?.Trim(),
            studentRegistration: request.StudentRegistration?.Trim());

        user.CreatedBy = actor;

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await _unitOfWork.Repository<User, Guid>().AddAsync(user, cancellationToken);

            var auditLog = new AuditLog(
                userId: _currentUserService.UserId ?? actor,
                action: AuditAction.Created,
                documentId: null,
                ipAddress: _currentUserService.IpAddress,
                details: $"Usuario institucional creado: '{user.Username}' con rol '{user.Role}'.");

            await _unitOfWork.Repository<AuditLog, Guid>().AddAsync(auditLog, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Institutional user '{Username}' created successfully by '{Actor}'", user.Username, actor);
            return Result<UserResponseDto>.Success(MapToDto(user), "Usuario creado exitosamente.");
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Error creating institutional user '{Username}'", request.Username);
            throw;
        }
    }

    public async Task<PagedResult<UserResponseDto>> GetUsersAsync(
        UserFilterDto filter, 
        CancellationToken cancellationToken = default)
    {
        var pageNumber = filter.PageNumber < 1 ? 1 : filter.PageNumber;
        var pageSize = filter.PageSize < 1 ? 10 : (filter.PageSize > 100 ? 100 : filter.PageSize);

        var (items, totalCount) = await _userRepository.SearchUsersAsync(
            filter.SearchTerm,
            filter.Role,
            filter.IsActive,
            pageNumber,
            pageSize,
            cancellationToken);

        var dtos = items.Select(MapToDto).ToList();
        return new PagedResult<UserResponseDto>(dtos, totalCount, pageNumber, pageSize);
    }

    public async Task<Result<UserResponseDto>> GetByIdAsync(
        Guid id, 
        CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Repository<User, Guid>().GetByIdAsync(id, cancellationToken);
        if (user is null || user.IsDeleted)
        {
            return Result<UserResponseDto>.Failure(
                "El usuario especificado no existe o fue eliminado.",
                "USER_NOT_FOUND");
        }

        return Result<UserResponseDto>.Success(MapToDto(user));
    }

    public async Task<Result<UserResponseDto>> UpdateUserAsync(
        Guid id, 
        UpdateUserRequestDto request, 
        CancellationToken cancellationToken = default)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            var firstError = validation.Errors[0].ErrorMessage;
            return Result<UserResponseDto>.Failure(
                firstError,
                "VALIDATION_FAILED",
                validation.Errors.Select(e => e.ErrorMessage));
        }

        var user = await _unitOfWork.Repository<User, Guid>().GetByIdAsync(id, cancellationToken);
        if (user is null || user.IsDeleted)
        {
            return Result<UserResponseDto>.Failure(
                "El usuario especificado no existe o fue eliminado.",
                "USER_NOT_FOUND");
        }

        var actor = _currentUserService.UserName ?? "Admin";
        user.UpdateProfile(request.FullName, request.Department, request.StudentRegistration, actor);

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            _unitOfWork.Repository<User, Guid>().Update(user);

            var auditLog = new AuditLog(
                userId: _currentUserService.UserId ?? actor,
                action: AuditAction.Updated,
                documentId: null,
                ipAddress: _currentUserService.IpAddress,
                details: $"Perfil de usuario '{user.Username}' actualizado.");

            await _unitOfWork.Repository<AuditLog, Guid>().AddAsync(auditLog, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Profile for user '{Username}' updated by '{Actor}'", user.Username, actor);
            return Result<UserResponseDto>.Success(MapToDto(user), "Perfil de usuario actualizado exitosamente.");
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Error updating profile for user ID '{UserId}'", id);
            throw;
        }
    }

    public async Task<Result<UserResponseDto>> ChangeUserRoleAsync(
        Guid id, 
        ChangeUserRoleRequestDto request, 
        CancellationToken cancellationToken = default)
    {
        var validation = await _roleValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            var firstError = validation.Errors[0].ErrorMessage;
            return Result<UserResponseDto>.Failure(
                firstError,
                "VALIDATION_FAILED",
                validation.Errors.Select(e => e.ErrorMessage));
        }

        var user = await _unitOfWork.Repository<User, Guid>().GetByIdAsync(id, cancellationToken);
        if (user is null || user.IsDeleted)
        {
            return Result<UserResponseDto>.Failure(
                "El usuario especificado no existe o fue eliminado.",
                "USER_NOT_FOUND");
        }

        var normalizedRole = UserRoles.Normalize(request.Role);
        var previousRole = user.Role;
        var actor = _currentUserService.UserName ?? "Admin";

        // Business rules to protect admin integrity:
        var isDemotingAdmin = previousRole.Equals(UserRoles.Admin, StringComparison.OrdinalIgnoreCase)
            && !normalizedRole.Equals(UserRoles.Admin, StringComparison.OrdinalIgnoreCase);

        if (isDemotingAdmin)
        {
            if (user.Username.Equals("admin", StringComparison.OrdinalIgnoreCase))
            {
                return Result<UserResponseDto>.Failure(
                    "No es posible revocar el rol de administrador a la cuenta principal del sistema.",
                    "CANNOT_DEMOTE_ROOT_ADMIN");
            }

            var isSelf = (Guid.TryParse(_currentUserService.UserId, out var currentGuid) && currentGuid == user.Id)
                || (!string.IsNullOrWhiteSpace(_currentUserService.UserName) && string.Equals(_currentUserService.UserName, user.Username, StringComparison.OrdinalIgnoreCase))
                || (!string.IsNullOrWhiteSpace(_currentUserService.UserId) && string.Equals(_currentUserService.UserId, user.Id.ToString(), StringComparison.OrdinalIgnoreCase));

            if (isSelf)
            {
                return Result<UserResponseDto>.Failure(
                    "No puedes revocar tu propio rol de administrador.",
                    "CANNOT_DEMOTE_SELF");
            }

            var hasOtherActiveAdmin = await _unitOfWork.Repository<User, Guid>().ExistsAsync(
                u => u.Id != id && u.Role == UserRoles.Admin && u.IsActive && !u.IsDeleted,
                cancellationToken);

            if (!hasOtherActiveAdmin)
            {
                return Result<UserResponseDto>.Failure(
                    "No es posible revocar el rol al único administrador activo del sistema.",
                    "LAST_ACTIVE_ADMIN");
            }
        }

        user.ChangeRole(normalizedRole, actor);

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            _unitOfWork.Repository<User, Guid>().Update(user);

            var auditLog = new AuditLog(
                userId: _currentUserService.UserId ?? actor,
                action: AuditAction.Updated,
                documentId: null,
                ipAddress: _currentUserService.IpAddress,
                details: $"Rol de usuario '{user.Username}' modificado de '{previousRole}' a '{normalizedRole}'.");

            await _unitOfWork.Repository<AuditLog, Guid>().AddAsync(auditLog, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Role for user '{Username}' changed from '{PreviousRole}' to '{NewRole}' by '{Actor}'",
                user.Username, previousRole, normalizedRole, actor);

            return Result<UserResponseDto>.Success(MapToDto(user), "Rol de usuario modificado exitosamente.");
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Error changing role for user ID '{UserId}'", id);
            throw;
        }
    }

    public async Task<Result<UserResponseDto>> ChangeUserStatusAsync(
        Guid id, 
        ChangeUserStatusRequestDto request, 
        CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Repository<User, Guid>().GetByIdAsync(id, cancellationToken);
        if (user is null || user.IsDeleted)
        {
            return Result<UserResponseDto>.Failure(
                "El usuario especificado no existe o fue eliminado.",
                "USER_NOT_FOUND");
        }

        var actor = _currentUserService.UserName ?? "Admin";

        // Business rules to protect admin integrity:
        if (!request.IsActive)
        {
            if (user.Username.Equals("admin", StringComparison.OrdinalIgnoreCase))
            {
                return Result<UserResponseDto>.Failure(
                    "No es posible desactivar la cuenta de administrador principal del sistema.",
                    "CANNOT_DEACTIVATE_ROOT_ADMIN");
            }

            var isSelf = (Guid.TryParse(_currentUserService.UserId, out var currentGuid) && currentGuid == user.Id)
                || (!string.IsNullOrWhiteSpace(_currentUserService.UserName) && string.Equals(_currentUserService.UserName, user.Username, StringComparison.OrdinalIgnoreCase))
                || (!string.IsNullOrWhiteSpace(_currentUserService.UserId) && string.Equals(_currentUserService.UserId, user.Id.ToString(), StringComparison.OrdinalIgnoreCase));

            if (isSelf)
            {
                return Result<UserResponseDto>.Failure(
                    "No puedes desactivar tu propia cuenta de usuario.",
                    "CANNOT_DEACTIVATE_SELF");
            }

            if (user.Role.Equals(UserRoles.Admin, StringComparison.OrdinalIgnoreCase))
            {
                var hasOtherActiveAdmin = await _unitOfWork.Repository<User, Guid>().ExistsAsync(
                    u => u.Id != id && u.Role == UserRoles.Admin && u.IsActive && !u.IsDeleted,
                    cancellationToken);

                if (!hasOtherActiveAdmin)
                {
                    return Result<UserResponseDto>.Failure(
                        "No es posible desactivar al único administrador activo del sistema.",
                        "LAST_ACTIVE_ADMIN");
                }
            }
        }

        user.ChangeStatus(request.IsActive, request.ResetLockout, actor);

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            _unitOfWork.Repository<User, Guid>().Update(user);

            var statusDesc = request.IsActive ? "activado" : "desactivado";
            if (request.ResetLockout)
            {
                statusDesc += " y bloqueo de intentos fallidos restablecido";
            }

            var auditLog = new AuditLog(
                userId: _currentUserService.UserId ?? actor,
                action: AuditAction.StatusChanged,
                documentId: null,
                ipAddress: _currentUserService.IpAddress,
                details: $"Estado de usuario '{user.Username}' modificado: {statusDesc}.");

            await _unitOfWork.Repository<AuditLog, Guid>().AddAsync(auditLog, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Status for user '{Username}' changed ({StatusDesc}) by '{Actor}'",
                user.Username, statusDesc, actor);

            return Result<UserResponseDto>.Success(MapToDto(user), "Estado de usuario actualizado exitosamente.");
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Error changing status for user ID '{UserId}'", id);
            throw;
        }
    }

    private static UserResponseDto MapToDto(User user)
    {
        return new UserResponseDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role,
            Department = user.Department,
            StudentRegistration = user.StudentRegistration,
            IsActive = user.IsActive,
            IsLockedOut = user.IsLockedOut(),
            LockoutEndUtc = user.LockoutEndUtc,
            FailedLoginAttempts = user.FailedLoginAttempts,
            CreatedAtUtc = user.CreatedAtUtc,
            CreatedBy = user.CreatedBy,
            LastModifiedAtUtc = user.LastModifiedAtUtc,
            LastModifiedBy = user.LastModifiedBy
        };
    }
}
