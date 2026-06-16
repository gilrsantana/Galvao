using Moq;
using Galvao.Application.Common.Interfaces;
using Galvao.Application.UseCases.Members.Commands;
using Galvao.Application.UseCases.Members.CommandHandlers;
using Galvao.Domain.Entities;
using Galvao.Shared;

namespace Galvao.UnitTests.Application;

public class PurgeUserCommandHandlerTests
{
    private readonly Mock<IMemberRepository> _memberRepositoryMock = new();
    private readonly Mock<IMemberContactRepository> _memberContactRepositoryMock = new();
    private readonly Mock<IRemovedUserRepository> _removedUserRepositoryMock = new();
    private readonly Mock<IEmailContactService> _emailContactServiceMock = new();
    private readonly Mock<IIdentityService> _identityServiceMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly PurgeUserCommandHandler _handler;

    public PurgeUserCommandHandlerTests()
    {
        _handler = new PurgeUserCommandHandler(
            _memberRepositoryMock.Object,
            _memberContactRepositoryMock.Object,
            _removedUserRepositoryMock.Object,
            _emailContactServiceMock.Object,
            _identityServiceMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnFailure_WhenMemberDoesNotExist()
    {
        // Arrange
        var memberId = Guid.NewGuid();
        var command = new PurgeUserCommand(memberId, "Password123!");

        _memberRepositoryMock
            .Setup(x => x.GetByIdAsync(memberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Member?)null);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Member.NotFound", result.Error.Code);

        _identityServiceMock.Verify(x => x.DeleteAccountAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnFailure_WhenCheckPasswordFails()
    {
        // Arrange
        var member = Member.Create("test@galvao.com", "DisplayName", "First", "Last", false, false).Value;
        var command = new PurgeUserCommand(member.Id, "WrongPassword!");
        var expectedError = new Error("Auth.InvalidCredentials", "Incorrect password.");

        _memberRepositoryMock
            .Setup(x => x.GetByIdAsync(member.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);

        _identityServiceMock
            .Setup(x => x.CheckPasswordAsync(member.Id, "WrongPassword!", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(expectedError));

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(expectedError, result.Error);

        _memberRepositoryMock.Verify(x => x.Remove(It.IsAny<Member>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnSuccess_AndPerformFullDeletions_WhenMemberContactExists()
    {
        // Arrange
        var member = Member.Create("test@galvao.com", "DisplayName", "First", "Last", true, false).Value;
        var command = new PurgeUserCommand(member.Id, "Password123!");
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

        _identityServiceMock
            .Setup(x => x.CheckPasswordAsync(member.Id, "Password123!", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _identityServiceMock
            .Setup(x => x.DeleteAccountAsync(member.Id, "Password123!", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.True(result.IsSuccess);

        _emailContactServiceMock.Verify(x => x.DeleteContactAsync("ext-123", It.IsAny<CancellationToken>()), Times.Once);
        _memberContactRepositoryMock.Verify(x => x.Remove(memberContact), Times.Once);
        _memberRepositoryMock.Verify(x => x.Remove(member), Times.Once);
        _identityServiceMock.Verify(x => x.DeleteAccountAsync(member.Id, "Password123!", It.IsAny<CancellationToken>()), Times.Once);
        
        _removedUserRepositoryMock.Verify(x => x.AddAsync(It.Is<RemovedUser>(ru => 
            ru.Name == "First Last" && 
            ru.Email == "test@galvao.com" &&
            ru.RemovedPersonalInformation == true &&
            ru.RemovedAccountData == true &&
            ru.RemovedMarketData == true &&
            ru.RemovedFromMailProvider == true
        ), It.IsAny<CancellationToken>()), Times.Once);

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnSuccess_AndPerformFullDeletions_WhenMemberContactDoesNotExist()
    {
        // Arrange
        var member = Member.Create("test@galvao.com", "DisplayName", "First", "Last", false, false).Value;
        var command = new PurgeUserCommand(member.Id, "Password123!");

        _memberRepositoryMock
            .Setup(x => x.GetByIdAsync(member.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);

        _memberContactRepositoryMock
            .Setup(x => x.GetByIdAsync(member.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MemberContact?)null);

        _identityServiceMock
            .Setup(x => x.CheckPasswordAsync(member.Id, "Password123!", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _identityServiceMock
            .Setup(x => x.DeleteAccountAsync(member.Id, "Password123!", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.True(result.IsSuccess);

        _emailContactServiceMock.Verify(x => x.DeleteContactAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _memberContactRepositoryMock.Verify(x => x.Remove(It.IsAny<MemberContact>()), Times.Never);
        _memberRepositoryMock.Verify(x => x.Remove(member), Times.Once);
        _identityServiceMock.Verify(x => x.DeleteAccountAsync(member.Id, "Password123!", It.IsAny<CancellationToken>()), Times.Once);
        
        _removedUserRepositoryMock.Verify(x => x.AddAsync(It.Is<RemovedUser>(ru => 
            ru.Name == "First Last" && 
            ru.Email == "test@galvao.com" &&
            ru.RemovedPersonalInformation == true &&
            ru.RemovedAccountData == true &&
            ru.RemovedMarketData == true &&
            ru.RemovedFromMailProvider == true
        ), It.IsAny<CancellationToken>()), Times.Once);

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
