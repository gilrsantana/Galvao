using Moq;
using Galvao.Application.Common.Interfaces;
using Galvao.Domain.MemberUserAggregate.Entities;
using Galvao.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Galvao.Infrastructure.Identity.Jobs;

namespace Galvao.UnitTests.Infrastructure;

public class IdentityServiceTests
{
    private readonly Mock<UserManager<Account>> _userManagerMock;
    private readonly Mock<RoleManager<Role>> _roleManagerMock;
    private readonly Mock<IMemberRepository> _memberRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IBackgroundJobClient> _backgroundJobClientMock = new();
    private readonly IOptions<JwtSettings> _jwtSettingsOptions;
    private readonly IdentityService _service;

    public IdentityServiceTests()
    {
        var userStoreMock = new Mock<IUserStore<Account>>();
        _userManagerMock = new Mock<UserManager<Account>>(userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        _userManagerMock
            .Setup(x => x.GenerateEmailConfirmationTokenAsync(It.IsAny<Account>()))
            .ReturnsAsync("dummy-token");

        var roleStoreMock = new Mock<IRoleStore<Role>>();
        _roleManagerMock = new Mock<RoleManager<Role>>(roleStoreMock.Object, null!, null!, null!, null!);

        var jwtSettings = new JwtSettings
        {
            Secret = "super_secret_key_12345678901234567890",
            Issuer = "galvao",
            Audience = "galvao",
            ExpiryInMinutes = 60
        };
        _jwtSettingsOptions = Options.Create(jwtSettings);

        _service = new IdentityService(
            _userManagerMock.Object,
            _roleManagerMock.Object,
            _memberRepositoryMock.Object,
            _jwtSettingsOptions,
            _backgroundJobClientMock.Object);
    }

    [Fact]
    public async Task RegisterAsync_ShouldReturnFailure_WhenEmailIsNotUnique()
    {
        // Arrange
        var email = "test@galvao.com";
        var account = Account.Create(Guid.NewGuid(), email);

        _userManagerMock
            .Setup(x => x.FindByEmailAsync(email))
            .ReturnsAsync(account);

        // Act
        var result = await _service.RegisterAsync(Guid.NewGuid(), email, "Password123!");

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Auth.EmailNotUnique", result.Error.Code);

        _userManagerMock.Verify(x => x.CreateAsync(It.IsAny<Account>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_ShouldReturnFailure_WhenAccountCreationFailed()
    {
        // Arrange
        var email = "test@galvao.com";
        var password = "Password123!";

        _userManagerMock
            .Setup(x => x.FindByEmailAsync(email))
            .ReturnsAsync((Account?)null);

        var identityError = new IdentityError { Description = "Password too short" };
        _userManagerMock
            .Setup(x => x.CreateAsync(It.IsAny<Account>(), password))
            .ReturnsAsync(IdentityResult.Failed(identityError));

        // Act
        var result = await _service.RegisterAsync(Guid.NewGuid(), email, password);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Auth.RegistrationFailed", result.Error.Code);
        Assert.Contains("Password too short", result.Error.Message);

        _memberRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Member>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_ShouldReturnFailure_WhenRoleAssignmentFailed()
    {
        // Arrange
        var email = "test@galvao.com";
        var password = "Password123!";

        _userManagerMock
            .Setup(x => x.FindByEmailAsync(email))
            .ReturnsAsync((Account?)null);

        _userManagerMock
            .Setup(x => x.CreateAsync(It.IsAny<Account>(), password))
            .ReturnsAsync(IdentityResult.Success);

        _roleManagerMock
            .Setup(x => x.RoleExistsAsync("User"))
            .ReturnsAsync(true);

        var identityError = new IdentityError { Description = "Role assignment failed" };
        _userManagerMock
            .Setup(x => x.AddToRoleAsync(It.IsAny<Account>(), "User"))
            .ReturnsAsync(IdentityResult.Failed(identityError));

        // Act
        var result = await _service.RegisterAsync(Guid.NewGuid(), email, password);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Auth.RoleAssignmentFailed", result.Error.Code);
        Assert.Contains("Role assignment failed", result.Error.Message);

        _memberRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Member>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_ShouldRegisterSuccessfully()
    {
        // Arrange
        var email = "test@galvao.com";
        var password = "Password123!";
        var userId = Guid.NewGuid();

        _userManagerMock
            .Setup(x => x.FindByEmailAsync(email))
            .ReturnsAsync((Account?)null);

        _userManagerMock
            .Setup(x => x.CreateAsync(It.IsAny<Account>(), password))
            .ReturnsAsync(IdentityResult.Success);

        _roleManagerMock
            .Setup(x => x.RoleExistsAsync("User"))
            .ReturnsAsync(true);

        _userManagerMock
            .Setup(x => x.AddToRoleAsync(It.IsAny<Account>(), "User"))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _service.RegisterAsync(userId, email, password);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(userId, result.Value);

        _userManagerMock.Verify(x => x.CreateAsync(It.Is<Account>(a => a.Id == userId && a.Email == email), password), Times.Once);

        // Verify email confirmation job was enqueued
        _backgroundJobClientMock.Verify(x => x.Create(
            It.Is<Job>(job => job.Method.Name == nameof(ISendEmailConfirmationJob.SendConfirmationEmailAsync) &&
                              (Guid)job.Args[0] == userId &&
                              ((string)job.Args[1]).Contains("confirm-email") &&
                              ((string)job.Args[1]).Contains("dummy-token")),
            It.IsAny<EnqueuedState>()), Times.Once);
    }

    [Fact]
    public async Task ConfirmEmailAsync_ShouldReturnFailure_WhenAccountNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userManagerMock
            .Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync((Account?)null);

        // Act
        var result = await _service.ConfirmEmailAsync(userId, "token");

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Auth.AccountNotFound", result.Error.Code);
    }

    [Fact]
    public async Task ConfirmEmailAsync_ShouldReturnFailure_WhenConfirmEmailFails()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var account = Account.Create(userId, "test@galvao.com");
        _userManagerMock
            .Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(account);

        var identityError = new IdentityError { Description = "Invalid token" };
        _userManagerMock
            .Setup(x => x.ConfirmEmailAsync(account, "token"))
            .ReturnsAsync(IdentityResult.Failed(identityError));

        // Act
        var result = await _service.ConfirmEmailAsync(userId, "token");

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Auth.ConfirmEmailFailed", result.Error.Code);
        Assert.Contains("Invalid token", result.Error.Message);
    }

    [Fact]
    public async Task ConfirmEmailAsync_ShouldReturnSuccess_WhenEmailConfirmedSuccessfully()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var account = Account.Create(userId, "test@galvao.com");
        _userManagerMock
            .Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(account);

        _userManagerMock
            .Setup(x => x.ConfirmEmailAsync(account, "token"))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _service.ConfirmEmailAsync(userId, "token");

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task ResendConfirmationEmailAsync_ShouldReturnFailure_WhenAccountNotFound()
    {
        // Arrange
        var email = "notfound@galvao.com";
        _userManagerMock
            .Setup(x => x.FindByEmailAsync(email))
            .ReturnsAsync((Account?)null);

        // Act
        var result = await _service.ResendConfirmationEmailAsync(email);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Auth.AccountNotFound", result.Error.Code);
    }

    [Fact]
    public async Task ResendConfirmationEmailAsync_ShouldReturnFailure_WhenEmailAlreadyConfirmed()
    {
        // Arrange
        var email = "test@galvao.com";
        var account = Account.Create(Guid.NewGuid(), email);
        _userManagerMock
            .Setup(x => x.FindByEmailAsync(email))
            .ReturnsAsync(account);

        _userManagerMock
            .Setup(x => x.IsEmailConfirmedAsync(account))
            .ReturnsAsync(true);

        // Act
        var result = await _service.ResendConfirmationEmailAsync(email);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Auth.EmailAlreadyConfirmed", result.Error.Code);
    }

    [Fact]
    public async Task ResendConfirmationEmailAsync_ShouldGenerateTokenAndEnqueueJob_WhenNotYetConfirmed()
    {
        // Arrange
        var email = "test@galvao.com";
        var userId = Guid.NewGuid();
        var account = Account.Create(userId, email);
        _userManagerMock
            .Setup(x => x.FindByEmailAsync(email))
            .ReturnsAsync(account);

        _userManagerMock
            .Setup(x => x.IsEmailConfirmedAsync(account))
            .ReturnsAsync(false);

        _userManagerMock
            .Setup(x => x.GenerateEmailConfirmationTokenAsync(account))
            .ReturnsAsync("new-dummy-token");

        // Act
        var result = await _service.ResendConfirmationEmailAsync(email);

        // Assert
        Assert.True(result.IsSuccess);

        _userManagerMock.Verify(x => x.GenerateEmailConfirmationTokenAsync(account), Times.Once);
        _backgroundJobClientMock.Verify(x => x.Create(
            It.Is<Job>(job => job.Method.Name == nameof(ISendEmailConfirmationJob.SendConfirmationEmailAsync) &&
                              (Guid)job.Args[0] == userId &&
                              ((string)job.Args[1]).Contains("confirm-email") &&
                              ((string)job.Args[1]).Contains("new-dummy-token")),
            It.IsAny<EnqueuedState>()), Times.Once);
    }
}
