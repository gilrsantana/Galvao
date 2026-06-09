using Moq;
using Galvao.Application.Common.Interfaces;
using Galvao.Application.UseCases.Roles.Commands;
using Galvao.Application.UseCases.Roles.CommandHandlers;
using Galvao.Shared;
using Xunit;

namespace Galvao.UnitTests.Application;

public class RemoveRoleCommandHandlerTests
{
    private readonly Mock<IRoleService> _roleServiceMock = new();
    private readonly RemoveRoleCommandHandler _handler;

    public RemoveRoleCommandHandlerTests()
    {
        _handler = new RemoveRoleCommandHandler(_roleServiceMock.Object);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnSuccess_WhenCommandIsValid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new RemoveRoleCommand(userId, "Admin");
        _roleServiceMock
            .Setup(x => x.RemoveRoleAsync(userId, "Admin", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.True(result.IsSuccess);
        _roleServiceMock.Verify(x => x.RemoveRoleAsync(userId, "Admin", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnFailure_WhenServiceFails()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new RemoveRoleCommand(userId, "Admin");
        var error = new Error("Auth.UserRoleNotFound", "User does not have role");
        _roleServiceMock
            .Setup(x => x.RemoveRoleAsync(userId, "Admin", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(error));

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Auth.UserRoleNotFound", result.Error.Code);
    }
}
