using Moq;
using Galvao.Application.Common.Interfaces;
using Galvao.Application.UseCases.Members.Commands;
using Galvao.Application.UseCases.Members.CommandHandlers;
using Galvao.Domain.Entities;
using Galvao.Shared;
using Xunit;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Galvao.UnitTests.Application;

public class UpdateMarketingPreferencesCommandHandlerTests
{
    private readonly Mock<IMemberRepository> _memberRepositoryMock = new();
    private readonly Mock<IMemberContactRepository> _memberContactRepositoryMock = new();
    private readonly Mock<IEmailContactService> _emailContactServiceMock = new();
    private readonly Mock<IConsentLogRepository> _consentLogRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly UpdateMarketingPreferencesCommandHandler _handler;

    public UpdateMarketingPreferencesCommandHandlerTests()
    {
        _handler = new UpdateMarketingPreferencesCommandHandler(
            _memberRepositoryMock.Object,
            _memberContactRepositoryMock.Object,
            _emailContactServiceMock.Object,
            _consentLogRepositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnFailure_WhenMemberDoesNotExist()
    {
        // Arrange
        var memberId = Guid.NewGuid();
        var command = new UpdateMarketingPreferencesCommand(memberId, true, false, "token", DateTime.UtcNow, "127.0.0.1", "Chrome");

        _memberRepositoryMock
            .Setup(x => x.GetByIdAsync(memberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Member?)null);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Member.NotFound", result.Error.Code);
        
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _consentLogRepositoryMock.Verify(x => x.AddAsync(It.IsAny<ConsentLog>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldOptInAndCreateContact_WhenContactDoesNotExist()
    {
        // Arrange
        var member = Member.Create("test@galvao.com", "Name", "First", "Last", false, false).Value;
        var command = new UpdateMarketingPreferencesCommand(member.Id, true, false, "token123", DateTime.UtcNow, "127.0.0.1", "Chrome");

        _memberRepositoryMock
            .Setup(x => x.GetByIdAsync(member.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);

        _memberContactRepositoryMock
            .Setup(x => x.GetByIdAsync(member.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MemberContact?)null);

        _emailContactServiceMock
            .Setup(x => x.CreateContactAsync("test@galvao.com", "First", "Last", true, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync("ext-123");

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(member.AcceptNews);
        Assert.False(member.AcceptPromo);
        Assert.False(member.PendingSync);

        _emailContactServiceMock.Verify(x => x.CreateContactAsync("test@galvao.com", "First", "Last", true, false, It.IsAny<CancellationToken>()), Times.Once);
        _memberContactRepositoryMock.Verify(x => x.AddAsync(It.Is<MemberContact>(c => c.ExternalContactId == "ext-123"), It.IsAny<CancellationToken>()), Times.Once);
        
        _consentLogRepositoryMock.Verify(x => x.AddAsync(It.Is<ConsentLog>(l => 
            l.MemberId == member.Id && 
            l.Action == "Opt-In" && 
            l.IpAddress == "127.0.0.1" && 
            l.Source == "Chrome" && 
            l.ConsentToken == "token123"), It.IsAny<CancellationToken>()), Times.Once);

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldOptInAndRestoreContact_WhenContactIsDeleted()
    {
        // Arrange
        var member = Member.Create("test@galvao.com", "Name", "First", "Last", false, false).Value;
        var command = new UpdateMarketingPreferencesCommand(member.Id, true, false, "token123", DateTime.UtcNow, "127.0.0.1", "Chrome");
        var memberContact = MemberContact.Create(member.Id, "DELETED", "test@galvao.com", true).Value;

        _memberRepositoryMock
            .Setup(x => x.GetByIdAsync(member.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);

        _memberContactRepositoryMock
            .Setup(x => x.GetByIdAsync(member.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(memberContact);

        _emailContactServiceMock
            .Setup(x => x.CreateContactAsync("test@galvao.com", "First", "Last", true, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync("new-ext-123");

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(member.AcceptNews);
        Assert.False(member.AcceptPromo);
        Assert.Equal("new-ext-123", memberContact.ExternalContactId);
        Assert.False(memberContact.Unsubscribed);
        Assert.False(member.PendingSync);

        _emailContactServiceMock.Verify(x => x.CreateContactAsync("test@galvao.com", "First", "Last", true, false, It.IsAny<CancellationToken>()), Times.Once);
        _memberContactRepositoryMock.Verify(x => x.Update(memberContact), Times.Once);
        _consentLogRepositoryMock.Verify(x => x.AddAsync(It.Is<ConsentLog>(l => l.Action == "Opt-In"), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldOptInAndUpdateExistingContact_WhenContactExistsAndActive()
    {
        // Arrange
        var member = Member.Create("test@galvao.com", "Name", "First", "Last", false, false).Value;
        var command = new UpdateMarketingPreferencesCommand(member.Id, true, false);
        var memberContact = MemberContact.Create(member.Id, "ext-123", "test@galvao.com", true).Value;

        _memberRepositoryMock
            .Setup(x => x.GetByIdAsync(member.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);

        _memberContactRepositoryMock
            .Setup(x => x.GetByIdAsync(member.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(memberContact);

        _emailContactServiceMock
            .Setup(x => x.UpdateContactAsync("ext-123", "First", "Last", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(member.AcceptNews);
        Assert.False(member.AcceptPromo);
        Assert.False(memberContact.Unsubscribed);
        Assert.False(member.PendingSync);

        _emailContactServiceMock.Verify(x => x.UpdateContactAsync("ext-123", "First", "Last", false, It.IsAny<CancellationToken>()), Times.Once);
        _memberContactRepositoryMock.Verify(x => x.Update(memberContact), Times.Once);
        _consentLogRepositoryMock.Verify(x => x.AddAsync(It.Is<ConsentLog>(l => l.Action == "Opt-In"), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldOptOutAndDeleteContact_WhenContactExists()
    {
        // Arrange
        var member = Member.Create("test@galvao.com", "Name", "First", "Last", true, false).Value;
        var command = new UpdateMarketingPreferencesCommand(member.Id, false, false, "token", DateTime.UtcNow, "192.168.0.1", "Firefox");
        var memberContact = MemberContact.Create(member.Id, "ext-123", "test@galvao.com", false).Value;

        _memberRepositoryMock
            .Setup(x => x.GetByIdAsync(member.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);

        _memberContactRepositoryMock
            .Setup(x => x.GetByIdAsync(member.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(memberContact);

        _emailContactServiceMock
            .Setup(x => x.DeleteContactAsync("ext-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(member.AcceptNews);
        Assert.False(member.AcceptPromo);
        Assert.Equal("DELETED", memberContact.ExternalContactId);
        Assert.True(memberContact.Unsubscribed);
        Assert.False(member.PendingSync);

        _emailContactServiceMock.Verify(x => x.DeleteContactAsync("ext-123", It.IsAny<CancellationToken>()), Times.Once);
        _memberContactRepositoryMock.Verify(x => x.Update(memberContact), Times.Once);
        _consentLogRepositoryMock.Verify(x => x.AddAsync(It.Is<ConsentLog>(l => 
            l.MemberId == member.Id && 
            l.Action == "Opt-Out" && 
            l.IpAddress == "192.168.0.1" && 
            l.Source == "Firefox"), It.IsAny<CancellationToken>()), Times.Once);

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldDoNothingWithContact_WhenContactDoesNotExistAndOptingOut()
    {
        // Arrange
        var member = Member.Create("test@galvao.com", "Name", "First", "Last", true, false).Value;
        var command = new UpdateMarketingPreferencesCommand(member.Id, false, false);

        _memberRepositoryMock
            .Setup(x => x.GetByIdAsync(member.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);

        _memberContactRepositoryMock
            .Setup(x => x.GetByIdAsync(member.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MemberContact?)null);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(member.AcceptNews);
        Assert.False(member.AcceptPromo);
        Assert.False(member.PendingSync);

        _emailContactServiceMock.Verify(x => x.DeleteContactAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _memberContactRepositoryMock.Verify(x => x.Update(It.IsAny<MemberContact>()), Times.Never);
        _consentLogRepositoryMock.Verify(x => x.AddAsync(It.Is<ConsentLog>(l => l.Action == "Opt-Out"), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldSucceedWithPendingSync_WhenEmailContactServiceCreateFails()
    {
        // Arrange
        var member = Member.Create("test@galvao.com", "Name", "First", "Last", false, false).Value;
        var command = new UpdateMarketingPreferencesCommand(member.Id, true, false);
        var expectedError = new Error("Service.Error", "Failed to create contact");

        _memberRepositoryMock
            .Setup(x => x.GetByIdAsync(member.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);

        _memberContactRepositoryMock
            .Setup(x => x.GetByIdAsync(member.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MemberContact?)null);

        _emailContactServiceMock
            .Setup(x => x.CreateContactAsync("test@galvao.com", "First", "Last", true, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<string>(expectedError));

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(member.AcceptNews);
        Assert.False(member.AcceptPromo);
        Assert.True(member.PendingSync);

        _memberContactRepositoryMock.Verify(x => x.AddAsync(It.IsAny<MemberContact>(), It.IsAny<CancellationToken>()), Times.Never);
        _consentLogRepositoryMock.Verify(x => x.AddAsync(It.Is<ConsentLog>(l => l.Action == "Opt-In"), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldSucceedWithPendingSync_WhenEmailContactServiceUpdateFails()
    {
        // Arrange
        var member = Member.Create("test@galvao.com", "Name", "First", "Last", true, false).Value;
        var command = new UpdateMarketingPreferencesCommand(member.Id, false, false);
        var memberContact = MemberContact.Create(member.Id, "ext-123", "test@galvao.com", false).Value;
        var expectedError = new Error("Service.Error", "Failed to delete contact");

        _memberRepositoryMock
            .Setup(x => x.GetByIdAsync(member.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);

        _memberContactRepositoryMock
            .Setup(x => x.GetByIdAsync(member.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(memberContact);

        _emailContactServiceMock
            .Setup(x => x.DeleteContactAsync("ext-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(expectedError));

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(member.AcceptNews);
        Assert.False(member.AcceptPromo);
        Assert.True(member.PendingSync);

        _consentLogRepositoryMock.Verify(x => x.AddAsync(It.Is<ConsentLog>(l => l.Action == "Opt-Out"), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnFailure_WhenDatabaseSaveThrowsException()
    {
        // Arrange
        var member = Member.Create("test@galvao.com", "Name", "First", "Last", false, false).Value;
        var command = new UpdateMarketingPreferencesCommand(member.Id, true, false);

        _memberRepositoryMock
            .Setup(x => x.GetByIdAsync(member.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);

        _memberContactRepositoryMock
            .Setup(x => x.GetByIdAsync(member.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MemberContact?)null);

        _emailContactServiceMock
            .Setup(x => x.CreateContactAsync("test@galvao.com", "First", "Last", true, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync("ext-123");

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database connection timeout"));

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Database.SaveFailed", result.Error.Code);
        
        _consentLogRepositoryMock.Verify(x => x.AddAsync(It.Is<ConsentLog>(l => l.Action == "Opt-In"), It.IsAny<CancellationToken>()), Times.Once);
    }
}
