using LexisCampusDMS.Application.Common;
using LexisCampusDMS.Application.DTOs;
using LexisCampusDMS.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LexisCampusDMS.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        IUserService userService,
        ILogger<UsersController> logger)
    {
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Creates a new institutional user with role and encrypted password.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(Result<UserResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Result<UserResponseDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateUser(
        [FromBody] CreateUserRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            return BadRequest(Result<UserResponseDto>.Failure("El cuerpo de la solicitud no puede estar vacío.", "INVALID_INPUT"));
        }

        var result = await _userService.CreateUserAsync(request, cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result);
    }

    /// <summary>
    /// Retrieves a paginated and filtered list of institutional users.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<UserResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetUsers(
        [FromQuery] UserFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        var result = await _userService.GetUsersAsync(filter, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves a specific user by identifier.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(Result<UserResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<UserResponseDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var result = await _userService.GetByIdAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Updates the general profile details of a user.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(Result<UserResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<UserResponseDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<UserResponseDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateUser(
        Guid id,
        [FromBody] UpdateUserRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            return BadRequest(Result<UserResponseDto>.Failure("El cuerpo de la solicitud no puede estar vacío.", "INVALID_INPUT"));
        }

        var result = await _userService.UpdateUserAsync(id, request, cancellationToken);
        if (!result.IsSuccess)
        {
            return result.ErrorCode == "USER_NOT_FOUND" ? NotFound(result) : BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Reassigns an institutional role to a user.
    /// </summary>
    [HttpPut("{id:guid}/role")]
    [ProducesResponseType(typeof(Result<UserResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<UserResponseDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<UserResponseDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ChangeRole(
        Guid id,
        [FromBody] ChangeUserRoleRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            return BadRequest(Result<UserResponseDto>.Failure("El cuerpo de la solicitud no puede estar vacío.", "INVALID_INPUT"));
        }

        var result = await _userService.ChangeUserRoleAsync(id, request, cancellationToken);
        if (!result.IsSuccess)
        {
            return result.ErrorCode == "USER_NOT_FOUND" ? NotFound(result) : BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Updates user active status and/or resets failed login lockout.
    /// </summary>
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(Result<UserResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<UserResponseDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<UserResponseDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ChangeStatus(
        Guid id,
        [FromBody] ChangeUserStatusRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            return BadRequest(Result<UserResponseDto>.Failure("El cuerpo de la solicitud no puede estar vacío.", "INVALID_INPUT"));
        }

        var result = await _userService.ChangeUserStatusAsync(id, request, cancellationToken);
        if (!result.IsSuccess)
        {
            return result.ErrorCode == "USER_NOT_FOUND" ? NotFound(result) : BadRequest(result);
        }

        return Ok(result);
    }
}
