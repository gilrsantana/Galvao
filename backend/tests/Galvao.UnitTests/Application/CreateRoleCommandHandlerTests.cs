using Moq;
using Galvao.Application.Common.Interfaces;
using Galvao.Application.UseCases.Roles.Commands;
using Galvao.Application.UseCases.Roles.CommandHandlers;
using Galvao.Shared;

namespace Galvao.UnitTests.Application;

public class CreateRoleCommandHandlerTests
{
    private readonly Mock<IRoleService> _roleServiceMock = new();
    private readonly CreateRoleCommandHandler _handler;

    public CreateRoleCommandHandlerTests()
    {
        _handler = new CreateRoleCommandHandler(_roleServiceMock.Object);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnSuccess_WhenCommandIsValid()
    {
        // Arrange
        var command = new CreateRoleCommand("Admin", "Administrator role");
        _roleServiceMock
            .Setup(x => x.CreateRoleAsync("Admin", "Administrator role", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.True(result.IsSuccess);
        _roleServiceMock.Verify(x => x.CreateRoleAsync("Admin", "Administrator role", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnFailure_WhenServiceFails()
    {
        // Arrange
        var command = new CreateRoleCommand("Admin", "Administrator role");
        var error = new Error("Auth.RoleAlreadyExists", "Role already exists");
        _roleServiceMock
            .Setup(x => x.CreateRoleAsync("Admin", "Administrator role", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(error));

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Auth.RoleAlreadyExists", result.Error.Code);
    }
}
