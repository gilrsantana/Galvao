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
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly UpdateMemberProfileCommandHandler _handler;

    public UpdateMemberProfileCommandHandlerTests()
    {
        _handler = new UpdateMemberProfileCommandHandler(
            _memberRepositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnSuccess_WhenCommandIsValid()
    {
        // Arrange
        var member = Member.Create("test@galvao.com", "Original Name", "OriginalFirst", "OriginalLast", false, false).Value;
        var memberId = member.Id;
        var command = new UpdateMemberProfileCommand(memberId, "Updated Name", "UpdatedFirst", "UpdatedLast");

        _memberRepositoryMock
            .Setup(x => x.GetByIdAsync(memberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Updated Name", member.DisplayName);
        Assert.Equal("UpdatedFirst", member.FirstName);
        Assert.Equal("UpdatedLast", member.LastName);
        
        _memberRepositoryMock.Verify(x => x.Update(member), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
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
