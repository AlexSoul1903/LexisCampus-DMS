using LexisCampusDMS.Application.Common;
using LexisCampusDMS.Application.DTOs;
using LexisCampusDMS.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LexisCampusDMS.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthService authService,
        ICurrentUserService currentUserService,
        ILogger<AuthController> logger)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Authenticates a user with username/email and password, returning an access token and refresh token.
    /// </summary>
    /// <param name="request">User credentials (username or email and password).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JWT Bearer token, refresh token and user information.</returns>
    /// <response code="200">User authenticated successfully.</response>
    /// <response code="400">Invalid input parameters.</response>
    /// <response code="401">Invalid credentials or user account temporarily locked out.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(Result<AuthResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<AuthResponseDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<AuthResponseDto>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequestDto request, 
        CancellationToken cancellationToken = default)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(Result<AuthResponseDto>.Failure(
                "El nombre de usuario/correo y la contraseña son obligatorios.",
                "INVALID_INPUT"));
        }

        var clientIp = _currentUserService.IpAddress;
        var result = await _authService.LoginAsync(request, clientIp, cancellationToken);

        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "ACCOUNT_LOCKED" || result.ErrorCode == "INVALID_CREDENTIALS" || result.ErrorCode == "ACCOUNT_INACTIVE")
            {
                return Unauthorized(result);
            }

            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Rotates a refresh token and generates a new access token for secure session continuation.
    /// </summary>
    /// <param name="request">Existing active refresh token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated JWT access token and rotated refresh token.</returns>
    /// <response code="200">Session refreshed successfully.</response>
    /// <response code="400">Missing refresh token.</response>
    /// <response code="401">Invalid, revoked or expired refresh token.</response>
    [HttpPost("refresh-token")]
    [ProducesResponseType(typeof(Result<AuthResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<AuthResponseDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<AuthResponseDto>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken(
        [FromBody] RefreshTokenRequestDto request, 
        CancellationToken cancellationToken = default)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return BadRequest(Result<AuthResponseDto>.Failure(
                "El token de actualización es obligatorio.",
                "INVALID_INPUT"));
        }

        var clientIp = _currentUserService.IpAddress;
        var result = await _authService.RefreshTokenAsync(request, clientIp, cancellationToken);

        if (!result.IsSuccess)
        {
            return Unauthorized(result);
        }

        return Ok(result);
    }
}
