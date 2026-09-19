using LexisCampusDMS.Application.Common;
using LexisCampusDMS.Application.DTOs;
using LexisCampusDMS.Application.Interfaces;
using LexisCampusDMS.Server.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LexisCampusDMS.UnitTests;

public class UsersControllerTests
{
    private readonly Mock<IUserService> _userServiceMock = new();
    private readonly Mock<ILogger<UsersController>> _loggerMock = new();
    private readonly UsersController _controller;

    public UsersControllerTests()
    {
        _controller = new UsersController(_userServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task CreateUser_ValidRequest_ReturnsCreatedAtAction()
    {
        // Arrange
        var request = new CreateUserRequestDto
        {
            Username = "cristal",
            Email = "cristal@lexiscampus.edu",
            Password = "Password123!",
            FullName = "Cristal",
            Role = "Registro"
        };

        var newId = Guid.NewGuid();
        var userResponse = new UserResponseDto
        {
            Id = newId,
            Username = "cristal",
            Email = "cristal@lexiscampus.edu",
            FullName = "Cristal",
            Role = "Registro",
            IsActive = true
        };

        _userServiceMock.Setup(s => s.CreateUserAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserResponseDto>.Success(userResponse));

        // Act
        var actionResult = await _controller.CreateUser(request);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(actionResult);
        var result = Assert.IsType<Result<UserResponseDto>>(createdResult.Value);
        Assert.True(result.IsSuccess);
        Assert.Equal(newId, result.Data!.Id);
        Assert.Equal("cristal", result.Data.Username);
        Assert.Equal("Cristal", result.Data.FullName);
    }

    [Fact]
    public async Task CreateUser_DuplicateUsername_ReturnsBadRequest()
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

        _userServiceMock.Setup(s => s.CreateUserAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserResponseDto>.Failure("El nombre de usuario ya está registrado en el sistema.", "USERNAME_ALREADY_EXISTS"));

        // Act
        var actionResult = await _controller.CreateUser(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult);
        var result = Assert.IsType<Result<UserResponseDto>>(badRequestResult.Value);
        Assert.False(result.IsSuccess);
        Assert.Equal("USERNAME_ALREADY_EXISTS", result.ErrorCode);
    }

    [Fact]
    public async Task GetUsers_ReturnsOk()
    {
        // Arrange
        var filter = new UserFilterDto();
        var usersList = new List<UserResponseDto>
        {
            new() { Id = Guid.NewGuid(), Username = "cristal", FullName = "Cristal", Role = "Registro" },
            new() { Id = Guid.NewGuid(), Username = "alexa", FullName = "Alexa", Role = "Admin" }
        };

        var pagedResult = new PagedResult<UserResponseDto>(usersList, 2, 1, 10);

        _userServiceMock.Setup(s => s.GetUsersAsync(filter, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var actionResult = await _controller.GetUsers(filter);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var result = Assert.IsType<PagedResult<UserResponseDto>>(okResult.Value);
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal("cristal", result.Data![0].Username);
        Assert.Equal("alexa", result.Data![1].Username);
    }

    [Fact]
    public async Task GetById_ExistingId_ReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var userResponse = new UserResponseDto { Id = id, Username = "alexa", FullName = "Alexa" };

        _userServiceMock.Setup(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserResponseDto>.Success(userResponse));

        // Act
        var actionResult = await _controller.GetById(id);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var result = Assert.IsType<Result<UserResponseDto>>(okResult.Value);
        Assert.True(result.IsSuccess);
        Assert.Equal(id, result.Data!.Id);
        Assert.Equal("alexa", result.Data.Username);
        Assert.Equal("Alexa", result.Data.FullName);
    }

    [Fact]
    public async Task GetById_NonExistingId_ReturnsNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        _userServiceMock.Setup(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserResponseDto>.Failure("Usuario no encontrado", "USER_NOT_FOUND"));

        // Act
        var actionResult = await _controller.GetById(id);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(actionResult);
        var result = Assert.IsType<Result<UserResponseDto>>(notFoundResult.Value);
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task ChangeRole_ValidRequest_ReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new ChangeUserRoleRequestDto { Role = "Auditor" };
        var userResponse = new UserResponseDto { Id = id, Username = "alexa", FullName = "Alexa", Role = "Auditor" };

        _userServiceMock.Setup(s => s.ChangeUserRoleAsync(id, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserResponseDto>.Success(userResponse));

        // Act
        var actionResult = await _controller.ChangeRole(id, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var result = Assert.IsType<Result<UserResponseDto>>(okResult.Value);
        Assert.True(result.IsSuccess);
        Assert.Equal("Auditor", result.Data!.Role);
        Assert.Equal("alexa", result.Data.Username);
    }

    [Fact]
    public async Task ChangeStatus_ValidRequest_ReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new ChangeUserStatusRequestDto { IsActive = false, ResetLockout = true };
        var userResponse = new UserResponseDto { Id = id, Username = "cristal", FullName = "Cristal", IsActive = false };

        _userServiceMock.Setup(s => s.ChangeUserStatusAsync(id, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserResponseDto>.Success(userResponse));

        // Act
        var actionResult = await _controller.ChangeStatus(id, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var result = Assert.IsType<Result<UserResponseDto>>(okResult.Value);
        Assert.True(result.IsSuccess);
        Assert.False(result.Data!.IsActive);
        Assert.Equal("cristal", result.Data.Username);
    }
}
