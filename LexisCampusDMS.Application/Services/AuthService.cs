using LexisCampusDMS.Application.Common;
using LexisCampusDMS.Application.DTOs;
using LexisCampusDMS.Application.Interfaces;
using LexisCampusDMS.Application.Options;
using LexisCampusDMS.Core.Domain.Entities;
using LexisCampusDMS.Core.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LexisCampusDMS.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasherService _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly JwtOptions _jwtOptions;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUnitOfWork unitOfWork,
        IPasswordHasherService passwordHasher,
        ITokenService tokenService,
        IOptions<JwtOptions> jwtOptions,
        ILogger<AuthService> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        _jwtOptions = jwtOptions?.Value ?? throw new ArgumentNullException(nameof(jwtOptions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<AuthResponseDto>> LoginAsync(
        LoginRequestDto request, 
        string? ipAddress = null, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Result<AuthResponseDto>.Failure(
                "El usuario y la contraseña son obligatorios.", 
                "MISSING_CREDENTIALS");
        }

        var normalizedInput = request.Username.Trim();
        var users = await _unitOfWork.Repository<User, Guid>().FindAsync(
            u => (u.Username == normalizedInput || u.Email == normalizedInput) && !u.IsDeleted,
            cancellationToken);

        var user = users.FirstOrDefault();
        if (user is null)
        {
            _logger.LogWarning("Failed login attempt for non-existent user '{Username}' from IP {IpAddress}", 
                normalizedInput, ipAddress ?? "Unknown");

            return Result<AuthResponseDto>.Failure(
                "Credenciales inválidas. Por favor verifique su usuario o correo y contraseña.", 
                "INVALID_CREDENTIALS");
        }

        // 1. Check brute-force lockout status
        if (user.IsLockedOut())
        {
            var remainingMinutes = Math.Max(1, (int)Math.Ceiling((user.LockoutEndUtc!.Value - DateTime.UtcNow).TotalMinutes));
            _logger.LogWarning("Login rejected for locked-out user '{Username}' from IP {IpAddress}. Remaining: {Minutes} min",
                user.Username, ipAddress ?? "Unknown", remainingMinutes);

            return Result<AuthResponseDto>.Failure(
                $"La cuenta está bloqueada temporalmente debido a 5 intentos fallidos consecutivos. Intente nuevamente en {remainingMinutes} minuto(s).",
                "ACCOUNT_LOCKED");
        }

        // 2. Check if user is active
        if (!user.IsActive)
        {
            _logger.LogWarning("Login rejected for disabled user '{Username}' from IP {IpAddress}",
                user.Username, ipAddress ?? "Unknown");

            return Result<AuthResponseDto>.Failure(
                "La cuenta de usuario se encuentra inactiva o deshabilitada.",
                "ACCOUNT_INACTIVE");
        }

        // 3. Cryptographic password verification
        var isPasswordValid = _passwordHasher.VerifyPassword(request.Password, user.PasswordHash);
        if (!isPasswordValid)
        {
            user.RecordFailedLogin();
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (user.IsLockedOut())
            {
                _logger.LogWarning("User '{Username}' has been locked out after 5 consecutive failed attempts from IP {IpAddress}",
                    user.Username, ipAddress ?? "Unknown");

                return Result<AuthResponseDto>.Failure(
                    "Cuenta bloqueada temporalmente tras 5 intentos fallidos consecutivos. Por seguridad, intente nuevamente en 15 minutos.",
                    "ACCOUNT_LOCKED");
            }

            var attemptsLeft = User.MaxFailedAccessAttempts - user.FailedLoginAttempts;
            _logger.LogWarning("Invalid password for user '{Username}' from IP {IpAddress}. Attempts left: {AttemptsLeft}",
                user.Username, ipAddress ?? "Unknown", attemptsLeft);

            return Result<AuthResponseDto>.Failure(
                $"Credenciales inválidas. Le quedan {attemptsLeft} intento(s) antes del bloqueo temporal.",
                "INVALID_CREDENTIALS");
        }

        // 4. Successful login: reset failed attempts counter
        user.ResetLockout();

        // 5. Generate Access Token & Refresh Token
        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshToken = _tokenService.GenerateRefreshToken(user.Id, ipAddress);

        await _unitOfWork.Repository<RefreshToken, Guid>().AddAsync(refreshToken, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User '{Username}' ({Role}) successfully authenticated from IP {IpAddress}",
            user.Username, user.Role, ipAddress ?? "Unknown");

        var response = new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken.Token,
            TokenType = "Bearer",
            ExpiresInSeconds = _jwtOptions.ExpiryMinutes * 60,
            User = new UserInfoDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FullName = user.FullName,
                Role = user.Role,
                Department = user.Department,
                StudentRegistration = user.StudentRegistration
            }
        };

        return Result<AuthResponseDto>.Success(response);
    }

    public async Task<Result<AuthResponseDto>> RefreshTokenAsync(
        RefreshTokenRequestDto request, 
        string? ipAddress = null, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return Result<AuthResponseDto>.Failure(
                "El token de actualización es obligatorio.",
                "MISSING_REFRESH_TOKEN");
        }

        var tokens = await _unitOfWork.Repository<RefreshToken, Guid>().FindAsync(
            r => r.Token == request.RefreshToken,
            cancellationToken);

        var existingToken = tokens.FirstOrDefault();
        if (existingToken is null)
        {
            _logger.LogWarning("Refresh token attempt with non-existent token from IP {IpAddress}", ipAddress ?? "Unknown");

            return Result<AuthResponseDto>.Failure(
                "Token de actualización no válido o no encontrado.",
                "INVALID_REFRESH_TOKEN");
        }

        // Check if token was revoked (possible reuse attack)
        if (existingToken.IsRevoked)
        {
            _logger.LogWarning("Attempted use of revoked refresh token for UserId {UserId} from IP {IpAddress}",
                existingToken.UserId, ipAddress ?? "Unknown");

            return Result<AuthResponseDto>.Failure(
                "El token de actualización ha sido revocado. Por favor inicie sesión nuevamente.",
                "REVOKED_REFRESH_TOKEN");
        }

        // Check expiration
        if (existingToken.IsExpired)
        {
            _logger.LogWarning("Attempted use of expired refresh token for UserId {UserId} from IP {IpAddress}",
                existingToken.UserId, ipAddress ?? "Unknown");

            return Result<AuthResponseDto>.Failure(
                "El token de actualización ha expirado. Por favor inicie sesión nuevamente.",
                "EXPIRED_REFRESH_TOKEN");
        }

        // Fetch associated user
        var user = await _unitOfWork.Repository<User, Guid>().GetByIdAsync(existingToken.UserId, cancellationToken);
        if (user is null || !user.IsActive || user.IsDeleted)
        {
            _logger.LogWarning("Refresh token for inactive or missing user {UserId} from IP {IpAddress}",
                existingToken.UserId, ipAddress ?? "Unknown");

            return Result<AuthResponseDto>.Failure(
                "El usuario asociado al token no existe o se encuentra inactivo.",
                "USER_NOT_FOUND");
        }

        // Rotate token: revoke old token and generate new one
        var newRefreshToken = _tokenService.GenerateRefreshToken(user.Id, ipAddress);
        existingToken.Revoke(ipAddress, newRefreshToken.Token);

        await _unitOfWork.Repository<RefreshToken, Guid>().AddAsync(newRefreshToken, cancellationToken);
        var newAccessToken = _tokenService.GenerateAccessToken(user);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Refresh token successfully rotated for user '{Username}' from IP {IpAddress}",
            user.Username, ipAddress ?? "Unknown");

        var response = new AuthResponseDto
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken.Token,
            TokenType = "Bearer",
            ExpiresInSeconds = _jwtOptions.ExpiryMinutes * 60,
            User = new UserInfoDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FullName = user.FullName,
                Role = user.Role,
                Department = user.Department,
                StudentRegistration = user.StudentRegistration
            }
        };

        return Result<AuthResponseDto>.Success(response);
    }
}
