using Moq;
using Galvao.Application.Common.Interfaces;
using Galvao.Application.UseCases.Members.Commands;
using Galvao.Application.UseCases.Members.CommandHandlers;
using Galvao.Domain.Entities;
using Galvao.Shared;

namespace Galvao.UnitTests.Application;

public class ChangeEmailCommandHandlerTests
{
    private readonly Mock<IMemberRepository> _memberRepositoryMock = new();
    private readonly Mock<IMemberContactRepository> _memberContactRepositoryMock = new();
    private readonly Mock<IEmailContactService> _emailContactServiceMock = new();
    private readonly Mock<IIdentityService> _identityServiceMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly ChangeEmailCommandHandler _handler;

    public ChangeEmailCommandHandlerTests()
    {
        _handler = new ChangeEmailCommandHandler(
            _memberRepositoryMock.Object,
            _memberContactRepositoryMock.Object,
            _emailContactServiceMock.Object,
            _identityServiceMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnFailure_WhenMemberDoesNotExist()
    {
        // Arrange
        var memberId = Guid.NewGuid();
        var command = new ChangeEmailCommand(memberId, "new@galvao.com");

        _memberRepositoryMock
            .Setup(x => x.GetByIdAsync(memberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Member?)null);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Member.NotFound", result.Error.Code);

        _identityServiceMock.Verify(x => x.ChangeEmailAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnSuccess_WhenEmailIsSame()
    {
        // Arrange
        var member = Member.Create("test@galvao.com", "Name", "First", "Last", false, false).Value;
        var command = new ChangeEmailCommand(member.Id, "test@galvao.com");

        _memberRepositoryMock
            .Setup(x => x.GetByIdAsync(member.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.True(result.IsSuccess);
        
        _identityServiceMock.Verify(x => x.ChangeEmailAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnFailure_WhenIdentityServiceChangeEmailFails()
    {
        // Arrange
        var member = Member.Create("old@galvao.com", "Name", "First", "Last", false, false).Value;
        var command = new ChangeEmailCommand(member.Id, "new@galvao.com");
        var expectedError = new Error("Identity.Error", "Failed to change email in Identity");

        _memberRepositoryMock
            .Setup(x => x.GetByIdAsync(member.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);

        _identityServiceMock
            .Setup(x => x.ChangeEmailAsync(member.Id, "new@galvao.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(expectedError));

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(expectedError, result.Error);

        _memberRepositoryMock.Verify(x => x.Update(It.IsAny<Member>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldUpdateEmailAndSyncWithResend_WhenPreferencesAreAccepted()
    {
        // Arrange
        var member = Member.Create("old@galvao.com", "Name", "First", "Last", true, false).Value;
        var command = new ChangeEmailCommand(member.Id, "new@galvao.com");
        var memberContact = MemberContact.Create(member.Id, "ext-123", "old@galvao.com", false).Value;

        _memberRepositoryMock
            .Setup(x => x.GetByIdAsync(member.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);

        _identityServiceMock
            .Setup(x => x.ChangeEmailAsync(member.Id, "new@galvao.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _memberContactRepositoryMock
            .Setup(x => x.GetByIdAsync(member.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(memberContact);

        _emailContactServiceMock
            .Setup(x => x.DeleteContactAsync("ext-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _emailContactServiceMock
            .Setup(x => x.CreateContactAsync("new@galvao.com", "First", "Last", true, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync("ext-456");

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("new@galvao.com", member.Email);
        Assert.Equal("new@galvao.com", memberContact.Email);
        Assert.Equal("ext-456", memberContact.ExternalContactId);
        Assert.False(memberContact.Unsubscribed);

        _identityServiceMock.Verify(x => x.ChangeEmailAsync(member.Id, "new@galvao.com", It.IsAny<CancellationToken>()), Times.Once);
        _emailContactServiceMock.Verify(x => x.DeleteContactAsync("ext-123", It.IsAny<CancellationToken>()), Times.Once);
        _emailContactServiceMock.Verify(x => x.CreateContactAsync("new@galvao.com", "First", "Last", true, false, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldUpdateEmailAndMarkContactDeleted_WhenPreferencesAreNotAccepted()
    {
        // Arrange
        var member = Member.Create("old@galvao.com", "Name", "First", "Last", false, false).Value;
        var command = new ChangeEmailCommand(member.Id, "new@galvao.com");
        var memberContact = MemberContact.Create(member.Id, "ext-123", "old@galvao.com", false).Value;

        _memberRepositoryMock
            .Setup(x => x.GetByIdAsync(member.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);

        _identityServiceMock
            .Setup(x => x.ChangeEmailAsync(member.Id, "new@galvao.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

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
        Assert.Equal("new@galvao.com", member.Email);
        Assert.Equal("new@galvao.com", memberContact.Email);
        Assert.Equal("DELETED", memberContact.ExternalContactId);
        Assert.True(memberContact.Unsubscribed);

        _identityServiceMock.Verify(x => x.ChangeEmailAsync(member.Id, "new@galvao.com", It.IsAny<CancellationToken>()), Times.Once);
        _emailContactServiceMock.Verify(x => x.DeleteContactAsync("ext-123", It.IsAny<CancellationToken>()), Times.Once);
        _emailContactServiceMock.Verify(x => x.CreateContactAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldUpdateEmailAndCreateContact_WhenContactDoesNotExistButPreferencesAreAccepted()
    {
        // Arrange
        var member = Member.Create("old@galvao.com", "Name", "First", "Last", true, false).Value;
        var command = new ChangeEmailCommand(member.Id, "new@galvao.com");

        _memberRepositoryMock
            .Setup(x => x.GetByIdAsync(member.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);

        _identityServiceMock
            .Setup(x => x.ChangeEmailAsync(member.Id, "new@galvao.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _memberContactRepositoryMock
            .Setup(x => x.GetByIdAsync(member.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MemberContact?)null);

        _emailContactServiceMock
            .Setup(x => x.CreateContactAsync("new@galvao.com", "First", "Last", true, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync("ext-456");

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("new@galvao.com", member.Email);

        _identityServiceMock.Verify(x => x.ChangeEmailAsync(member.Id, "new@galvao.com", It.IsAny<CancellationToken>()), Times.Once);
        _emailContactServiceMock.Verify(x => x.DeleteContactAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _emailContactServiceMock.Verify(x => x.CreateContactAsync("new@galvao.com", "First", "Last", true, false, It.IsAny<CancellationToken>()), Times.Once);
        _memberContactRepositoryMock.Verify(x => x.AddAsync(It.Is<MemberContact>(c => c.ExternalContactId == "ext-456"), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldUpdateEmailAndDoNothingWithContact_WhenContactDoesNotExistAndPreferencesAreNotAccepted()
    {
        // Arrange
        var member = Member.Create("old@galvao.com", "Name", "First", "Last", false, false).Value;
        var command = new ChangeEmailCommand(member.Id, "new@galvao.com");

        _memberRepositoryMock
            .Setup(x => x.GetByIdAsync(member.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);

        _identityServiceMock
            .Setup(x => x.ChangeEmailAsync(member.Id, "new@galvao.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _memberContactRepositoryMock
            .Setup(x => x.GetByIdAsync(member.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MemberContact?)null);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("new@galvao.com", member.Email);

        _identityServiceMock.Verify(x => x.ChangeEmailAsync(member.Id, "new@galvao.com", It.IsAny<CancellationToken>()), Times.Once);
        _emailContactServiceMock.Verify(x => x.DeleteContactAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _emailContactServiceMock.Verify(x => x.CreateContactAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
        _memberContactRepositoryMock.Verify(x => x.AddAsync(It.IsAny<MemberContact>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnFailure_WhenDeleteContactFails()
    {
        // Arrange
        var member = Member.Create("old@galvao.com", "Name", "First", "Last", true, false).Value;
        var command = new ChangeEmailCommand(member.Id, "new@galvao.com");
        var memberContact = MemberContact.Create(member.Id, "ext-123", "old@galvao.com", false).Value;
        var expectedError = new Error("Service.Error", "Failed to delete old contact");

        _memberRepositoryMock
            .Setup(x => x.GetByIdAsync(member.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);

        _identityServiceMock
            .Setup(x => x.ChangeEmailAsync(member.Id, "new@galvao.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _memberContactRepositoryMock
            .Setup(x => x.GetByIdAsync(member.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(memberContact);

        _emailContactServiceMock
            .Setup(x => x.DeleteContactAsync("ext-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(expectedError));

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(expectedError, result.Error);

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnFailure_WhenCreateContactFails()
    {
        // Arrange
        var member = Member.Create("old@galvao.com", "Name", "First", "Last", true, false).Value;
        var command = new ChangeEmailCommand(member.Id, "new@galvao.com");
        var memberContact = MemberContact.Create(member.Id, "ext-123", "old@galvao.com", false).Value;
        var expectedError = new Error("Service.Error", "Failed to create contact");

        _memberRepositoryMock
            .Setup(x => x.GetByIdAsync(member.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);

        _identityServiceMock
            .Setup(x => x.ChangeEmailAsync(member.Id, "new@galvao.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _memberContactRepositoryMock
            .Setup(x => x.GetByIdAsync(member.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(memberContact);

        _emailContactServiceMock
            .Setup(x => x.DeleteContactAsync("ext-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _emailContactServiceMock
            .Setup(x => x.CreateContactAsync("new@galvao.com", "First", "Last", true, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<string>(expectedError));

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(expectedError, result.Error);

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
