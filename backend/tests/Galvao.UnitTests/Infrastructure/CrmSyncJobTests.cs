using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Xunit;
using Galvao.Application.Common.Interfaces;
using Galvao.Domain.Entities;
using Galvao.Infrastructure.Identity.Jobs;
using Galvao.Shared;

namespace Galvao.UnitTests.Infrastructure;

public class CrmSyncJobTests
{
    private readonly Mock<IEmailContactService> _emailContactServiceMock = new();
    private readonly Mock<IMemberContactRepository> _memberContactRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly CrmSyncJob _job;

    public CrmSyncJobTests()
    {
        _job = new CrmSyncJob(
            _emailContactServiceMock.Object,
            _memberContactRepositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task SyncContactAsync_ShouldDoNothing_WhenNoMarketingPreferencesAccepted()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "test@galvao.com";
        var firstName = "First";
        var lastName = "Last";

        // Act
        await _job.SyncContactAsync(userId, email, firstName, lastName, false, false, CancellationToken.None);

        // Assert
        _emailContactServiceMock.Verify(
            x => x.CreateContactAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _memberContactRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<MemberContact>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SyncContactAsync_ShouldCallResendAndAddContactAndSave_WhenMarketingPreferencesAccepted()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "test@galvao.com";
        var firstName = "First";
        var lastName = "Last";
        var externalContactId = "ext-contact-id";

        _emailContactServiceMock
            .Setup(x => x.CreateContactAsync(email, firstName, lastName, true, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(externalContactId));

        // Act
        await _job.SyncContactAsync(userId, email, firstName, lastName, true, false, CancellationToken.None);

        // Assert
        _emailContactServiceMock.Verify(
            x => x.CreateContactAsync(email, firstName, lastName, true, false, It.IsAny<CancellationToken>()),
            Times.Once);
        
        _memberContactRepositoryMock.Verify(
            x => x.AddAsync(It.Is<MemberContact>(c => c.Id == userId && c.ExternalContactId == externalContactId && c.Email == email && !c.Unsubscribed), It.IsAny<CancellationToken>()),
            Times.Once);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SyncContactAsync_ShouldThrowException_WhenResendApiFails()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "test@galvao.com";
        var firstName = "First";
        var lastName = "Last";
        var error = new Error("Resend.Error", "API request failed");

        _emailContactServiceMock
            .Setup(x => x.CreateContactAsync(email, firstName, lastName, true, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<string>(error));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _job.SyncContactAsync(userId, email, firstName, lastName, true, false, CancellationToken.None));

        Assert.Contains("Resend API call failed", exception.Message);
        Assert.Contains("API request failed", exception.Message);

        _memberContactRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<MemberContact>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SyncContactAsync_ShouldThrowException_WhenMemberContactCreateFails()
    {
        // Arrange
        var userId = Guid.Empty; // Invalid member id
        var email = "test@galvao.com";
        var firstName = "First";
        var lastName = "Last";
        var externalContactId = "ext-contact-id";

        _emailContactServiceMock
            .Setup(x => x.CreateContactAsync(email, firstName, lastName, true, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(externalContactId));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _job.SyncContactAsync(userId, email, firstName, lastName, true, false, CancellationToken.None));

        Assert.Contains("Failed to create MemberContact domain model", exception.Message);

        _memberContactRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<MemberContact>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
