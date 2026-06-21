using Moq;
using Galvao.Application.Common.Interfaces;
using Galvao.Domain.MemberContactAggregate.Enums;
using Galvao.Infrastructure.Identity;
using Galvao.Infrastructure.Identity.Jobs;
using Galvao.Shared;
using Microsoft.AspNetCore.Identity;

namespace Galvao.UnitTests.Infrastructure;

public class SendEmailConfirmationJobTests
{
    private readonly Mock<UserManager<Account>> _userManagerMock;
    private readonly Mock<IEmailSender> _emailSenderMock = new();
    private readonly SendEmailConfirmationJob _job;

    public SendEmailConfirmationJobTests()
    {
        var userStoreMock = new Mock<IUserStore<Account>>();
        _userManagerMock = new Mock<UserManager<Account>>(userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        _job = new SendEmailConfirmationJob(
            _userManagerMock.Object,
            _emailSenderMock.Object);
    }

    [Fact]
    public async Task SendConfirmationEmailAsync_ShouldThrowException_WhenUserNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userManagerMock
            .Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync((Account?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _job.SendConfirmationEmailAsync(userId, "http://confirm", CancellationToken.None));

        Assert.Contains("not found", exception.Message);
        _emailSenderMock.Verify(x => x.SendEmailAsync(
            It.IsAny<List<string>>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<Guid?>(),
            It.IsAny<ETypeOfMessage?>(),
            It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SendConfirmationEmailAsync_ShouldThrowException_WhenUserHasNoEmail()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var account = Account.Create(userId, ""); // Empty email
        _userManagerMock
            .Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(account);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _job.SendConfirmationEmailAsync(userId, "http://confirm", CancellationToken.None));

        Assert.Contains("does not have a valid email address", exception.Message);
        _emailSenderMock.Verify(x => x.SendEmailAsync(
            It.IsAny<List<string>>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<Guid?>(),
            It.IsAny<ETypeOfMessage?>(),
            It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SendConfirmationEmailAsync_ShouldThrowException_WhenEmailSenderFails()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "test@galvao.com";
        var account = Account.Create(userId, email);
        var confirmationLink = "http://confirm";

        _userManagerMock
            .Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(account);

        var error = new Error("Email.Failed", "SMTP server down");
        _emailSenderMock
            .Setup(x => x.SendEmailAsync(
                It.Is<List<string>>(l => l.Contains(email)),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Guid?>(),
                It.IsAny<ETypeOfMessage?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(error));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _job.SendConfirmationEmailAsync(userId, confirmationLink, CancellationToken.None));

        Assert.Contains("Failed to send confirmation email", exception.Message);
        Assert.Contains("SMTP server down", exception.Message);
    }

    [Fact]
    public async Task SendConfirmationEmailAsync_ShouldSendSuccessfully_WhenAllParametersAreCorrect()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "test@galvao.com";
        var account = Account.Create(userId, email);
        var confirmationLink = "http://confirm";

        _userManagerMock
            .Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(account);

        _emailSenderMock
            .Setup(x => x.SendEmailAsync(
                It.Is<List<string>>(l => l.Contains(email)),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Guid?>(),
                It.IsAny<ETypeOfMessage?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        await _job.SendConfirmationEmailAsync(userId, confirmationLink, CancellationToken.None);

        // Assert
        _emailSenderMock.Verify(x => x.SendEmailAsync(
            It.Is<List<string>>(l => l.Contains(email)),
            "Confirme seu endereço de e-mail",
            It.Is<string>(html => html.Contains(confirmationLink)),
            userId,
            ETypeOfMessage.EmailConfirmation,
            It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
