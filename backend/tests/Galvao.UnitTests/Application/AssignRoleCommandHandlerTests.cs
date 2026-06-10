using Moq;
using Galvao.Application.Common.Interfaces;
using Galvao.Application.UseCases.Roles.Commands;
using Galvao.Application.UseCases.Roles.CommandHandlers;
using Galvao.Shared;
using Xunit;

namespace Galvao.UnitTests.Application;

public class AssignRoleCommandHandlerTests
{
    private readonly Mock<IRoleService> _roleServiceMock = new();
    private readonly AssignRoleCommandHandler _handler;

    public AssignRoleCommandHandlerTests()
    {
        _handler = new AssignRoleCommandHandler(_roleServiceMock.Object);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnSuccess_WhenCommandIsValid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new AssignRoleCommand(userId, "Admin");
        _roleServiceMock
            .Setup(x => x.AssignRoleAsync(userId, "Admin", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.True(result.IsSuccess);
        _roleServiceMock.Verify(x => x.AssignRoleAsync(userId, "Admin", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnFailure_WhenServiceFails()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new AssignRoleCommand(userId, "Admin");
        var error = new Error("Auth.UserNotFound", "User not found");
        _roleServiceMock
            .Setup(x => x.AssignRoleAsync(userId, "Admin", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(error));

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Auth.UserNotFound", result.Error.Code);
    }
}
