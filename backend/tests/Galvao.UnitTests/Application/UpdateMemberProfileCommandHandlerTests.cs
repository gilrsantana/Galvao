using Moq;
using Galvao.Application.Common.Interfaces;
using Galvao.Application.UseCases.Members.Commands;
using Galvao.Application.UseCases.Members.CommandHandlers;
using Galvao.Domain.Entities;
using Galvao.Shared;
using Xunit;

namespace Galvao.UnitTests.Application;

public class UpdateMemberProfileCommandHandlerTests
{
    private readonly Mock<IMemberRepository> _memberRepositoryMock = new();
    private readonly Mock<IMemberContactRepository> _memberContactRepositoryMock = new();
    private readonly Mock<IEmailContactService> _emailContactServiceMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly UpdateMemberProfileCommandHandler _handler;

    public UpdateMemberProfileCommandHandlerTests()
    {
        _handler = new UpdateMemberProfileCommandHandler(
            _memberRepositoryMock.Object,
            _memberContactRepositoryMock.Object,
            _emailContactServiceMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnSuccess_WhenCommandIsValidAndNameHasNotChanged()
    {
        // Arrange
        var member = Member.Create("test@galvao.com", "Original Name", "First", "Last", false, false).Value;
        var memberId = member.Id;
        var command = new UpdateMemberProfileCommand(memberId, "Updated Name", "First", "Last");

        _memberRepositoryMock
            .Setup(x => x.GetByIdAsync(memberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Updated Name", member.DisplayName);
        Assert.Equal("First", member.FirstName);
        Assert.Equal("Last", member.LastName);
        
        _memberRepositoryMock.Verify(x => x.Update(member), Times.Once);
        _memberContactRepositoryMock.Verify(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnSuccessAndSyncContact_WhenNameChangesAndContactExists()
    {
        // Arrange
        var member = Member.Create("test@galvao.com", "Original Name", "OriginalFirst", "OriginalLast", false, false).Value;
        var memberId = member.Id;
        var command = new UpdateMemberProfileCommand(memberId, "Updated Name", "UpdatedFirst", "UpdatedLast");
        var memberContact = MemberContact.Create(memberId, "ext-123", "test@galvao.com", false).Value;

        _memberRepositoryMock
            .Setup(x => x.GetByIdAsync(memberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);

        _memberContactRepositoryMock
            .Setup(x => x.GetByIdAsync(memberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(memberContact);

        _emailContactServiceMock
            .Setup(x => x.UpdateContactAsync("ext-123", "UpdatedFirst", "UpdatedLast", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Updated Name", member.DisplayName);
        Assert.Equal("UpdatedFirst", member.FirstName);
        Assert.Equal("UpdatedLast", member.LastName);
        
        _memberRepositoryMock.Verify(x => x.Update(member), Times.Once);
        _emailContactServiceMock.Verify(x => x.UpdateContactAsync("ext-123", "UpdatedFirst", "UpdatedLast", false, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnSuccessAndNotSyncContact_WhenNameChangesButContactDoesNotExist()
    {
        // Arrange
        var member = Member.Create("test@galvao.com", "Original Name", "OriginalFirst", "OriginalLast", false, false).Value;
        var memberId = member.Id;
        var command = new UpdateMemberProfileCommand(memberId, "Updated Name", "UpdatedFirst", "UpdatedLast");

        _memberRepositoryMock
            .Setup(x => x.GetByIdAsync(memberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);

        _memberContactRepositoryMock
            .Setup(x => x.GetByIdAsync(memberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MemberContact?)null);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.True(result.IsSuccess);
        
        _memberRepositoryMock.Verify(x => x.Update(member), Times.Once);
        _emailContactServiceMock.Verify(x => x.UpdateContactAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnFailure_WhenEmailContactServiceUpdateFails()
    {
        // Arrange
        var member = Member.Create("test@galvao.com", "Original Name", "OriginalFirst", "OriginalLast", false, false).Value;
        var memberId = member.Id;
        var command = new UpdateMemberProfileCommand(memberId, "Updated Name", "UpdatedFirst", "UpdatedLast");
        var memberContact = MemberContact.Create(memberId, "ext-123", "test@galvao.com", false).Value;
        var expectedError = new Error("Service.Error", "Failed to update contact");

        _memberRepositoryMock
            .Setup(x => x.GetByIdAsync(memberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);

        _memberContactRepositoryMock
            .Setup(x => x.GetByIdAsync(memberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(memberContact);

        _emailContactServiceMock
            .Setup(x => x.UpdateContactAsync("ext-123", "UpdatedFirst", "UpdatedLast", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(expectedError));

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(expectedError, result.Error);
        
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnFailure_WhenMemberDoesNotExist()
    {
        // Arrange
        var memberId = Guid.NewGuid();
        var command = new UpdateMemberProfileCommand(memberId, "Updated Name", "UpdatedFirst", "UpdatedLast");

        _memberRepositoryMock
            .Setup(x => x.GetByIdAsync(memberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Member?)null);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Member.NotFound", result.Error.Code);

        _memberRepositoryMock.Verify(x => x.Update(It.IsAny<Member>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnFailure_WhenDisplayNameIsEmpty()
    {
        // Arrange
        var member = Member.Create("test@galvao.com", "Original Name", "First", "Last", false, false).Value;
        var memberId = member.Id;
        var command = new UpdateMemberProfileCommand(memberId, "", "First", "Last"); // Invalid name

        _memberRepositoryMock
            .Setup(x => x.GetByIdAsync(memberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Member.DisplayNameRequired", result.Error.Code);

        _memberRepositoryMock.Verify(x => x.Update(It.IsAny<Member>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnFailure_WhenFirstNameIsEmpty()
    {
        // Arrange
        var member = Member.Create("test@galvao.com", "Original Name", "First", "Last", false, false).Value;
        var memberId = member.Id;
        var command = new UpdateMemberProfileCommand(memberId, "DisplayName", "", "Last"); // Invalid first name

        _memberRepositoryMock
            .Setup(x => x.GetByIdAsync(memberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Member.FirstNameRequired", result.Error.Code);

        _memberRepositoryMock.Verify(x => x.Update(It.IsAny<Member>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnFailure_WhenLastNameIsEmpty()
    {
        // Arrange
        var member = Member.Create("test@galvao.com", "Original Name", "First", "Last", false, false).Value;
        var memberId = member.Id;
        var command = new UpdateMemberProfileCommand(memberId, "DisplayName", "First", ""); // Invalid last name

        _memberRepositoryMock
            .Setup(x => x.GetByIdAsync(memberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Member.LastNameRequired", result.Error.Code);

        _memberRepositoryMock.Verify(x => x.Update(It.IsAny<Member>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
