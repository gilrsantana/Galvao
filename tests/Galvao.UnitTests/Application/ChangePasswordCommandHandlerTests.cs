using Moq;
using Galvao.Application.Common.Interfaces;
using Galvao.Application.UseCases.Members.Commands;
using Galvao.Application.UseCases.Members.CommandHandlers;
using Galvao.Shared;
using Xunit;

namespace Galvao.UnitTests.Application;

public class ChangePasswordCommandHandlerTests
{
    private readonly Mock<IIdentityService> _identityServiceMock = new();
    private readonly ChangePasswordCommandHandler _handler;

    public ChangePasswordCommandHandlerTests()
    {
        _handler = new ChangePasswordCommandHandler(_identityServiceMock.Object);
    }

    [Fact]
    public async Task HandleAsync_ShouldCallIdentityServiceChangePassword()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var currentPassword = "CurrentPassword123!";
        var newPassword = "NewPassword123!";
        var command = new ChangePasswordCommand(userId, currentPassword, newPassword);

        _identityServiceMock
            .Setup(x => x.ChangePasswordAsync(userId, currentPassword, newPassword, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.True(result.IsSuccess);
        _identityServiceMock.Verify(x => x.ChangePasswordAsync(userId, currentPassword, newPassword, It.IsAny<CancellationToken>()), Times.Once);
    }
}
