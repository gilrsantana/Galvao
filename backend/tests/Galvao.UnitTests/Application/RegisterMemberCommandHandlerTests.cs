using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Galvao.Application.ApplicationJobs.Interfaces;
using Galvao.Application.Common.Interfaces;
using Galvao.Application.UseCases.Members.CommandHandlers;
using Galvao.Application.UseCases.Members.Commands;
using Galvao.Domain.MemberUserAggregate.Entities;
using Galvao.Shared;
using Moq;

namespace Galvao.UnitTests.Application;

public class RegisterMemberCommandHandlerTests
{
    private readonly Mock<IMemberRepository> _memberRepositoryMock = new();
    private readonly Mock<IIdentityService> _identityServiceMock = new();
    private readonly Mock<IBackgroundJobService> _backgroundJobServiceMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly RegisterMemberCommandHandler _handler;

    public RegisterMemberCommandHandlerTests()
    {
        _handler = new RegisterMemberCommandHandler(
            _identityServiceMock.Object,
            _unitOfWorkMock.Object,
            _memberRepositoryMock.Object,
            _backgroundJobServiceMock.Object);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnFailure_WhenEmailIsNotUnique()
    {
        // Arrange
        var command = new RegisterMemberCommand(
            "test@galvao.com", "Password123!", "Display", "First", "Last", false, false);
        var expectedError = new Error("Auth.EmailNotUnique", "Email is already registered.");

        _identityServiceMock
            .Setup(x => x.CheckEmailUniquenessAsync(command.Email))
            .ReturnsAsync(Result.Failure(expectedError));

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(expectedError, result.Error);

        _identityServiceMock.Verify(x => x.RegisterAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _memberRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Member>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnFailure_WhenMemberValidationFails()
    {
        // Arrange
        var command = new RegisterMemberCommand(
            "invalid-email", "Password123!", "Display", "First", "Last", false, false);

        _identityServiceMock
            .Setup(x => x.CheckEmailUniquenessAsync(command.Email))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Member.InvalidEmail", result.Error.Code);

        _identityServiceMock.Verify(x => x.RegisterAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _memberRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Member>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnFailure_WhenIdentityServiceRegistrationFails()
    {
        // Arrange
        var command = new RegisterMemberCommand(
            "test@galvao.com", "Password123!", "Display", "First", "Last", false, false);
        var expectedError = new Error("Auth.RegistrationFailed", "Could not create account");

        _identityServiceMock
            .Setup(x => x.CheckEmailUniquenessAsync(command.Email))
            .ReturnsAsync(Result.Success());

        _identityServiceMock
            .Setup(x => x.RegisterAsync(It.IsAny<Guid>(), command.Email, command.Password, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<Guid>(expectedError));

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(expectedError, result.Error);

        _memberRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Member>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldRegisterSuccessfully_AndNotSync_WhenMarketingPreferencesNotAccepted()
    {
        // Arrange
        var command = new RegisterMemberCommand(
            "test@galvao.com", "Password123!", "Display", "First", "Last", false, false);

        _identityServiceMock
            .Setup(x => x.CheckEmailUniquenessAsync(command.Email))
            .ReturnsAsync(Result.Success());

        _identityServiceMock
            .Setup(x => x.RegisterAsync(It.IsAny<Guid>(), command.Email, command.Password, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid userId, string e, string p, CancellationToken c) => Result.Success(userId));

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value);

        _memberRepositoryMock.Verify(x => x.AddAsync(It.Is<Member>(m => m.Id == result.Value && m.Email == command.Email), It.IsAny<CancellationToken>()), Times.Once);
        _backgroundJobServiceMock.Verify(x => x.Enqueue(It.IsAny<Expression<Func<ICrmSyncJob, Task>>>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldRegisterSuccessfully_AndSyncContact_WhenMarketingPreferencesAccepted()
    {
        // Arrange
        var command = new RegisterMemberCommand(
            "test@galvao.com", "Password123!", "Display", "First", "Last", true, false);

        _identityServiceMock
            .Setup(x => x.CheckEmailUniquenessAsync(command.Email))
            .ReturnsAsync(Result.Success());

        _identityServiceMock
            .Setup(x => x.RegisterAsync(It.IsAny<Guid>(), command.Email, command.Password, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid userId, string e, string p, CancellationToken c) => Result.Success(userId));

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value);

        _memberRepositoryMock.Verify(x => x.AddAsync(It.Is<Member>(m => m.Id == result.Value && m.Email == command.Email), It.IsAny<CancellationToken>()), Times.Once);
        _backgroundJobServiceMock.Verify(x => x.Enqueue(It.IsAny<Expression<Func<ICrmSyncJob, Task>>>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
