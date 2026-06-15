using Moq;
using Galvao.Application.Common.Interfaces;
using Galvao.Infrastructure.Identity;
using Galvao.Domain.Entities;
using Galvao.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Xunit;
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
            _unitOfWorkMock.Object,
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
        var result = await _service.RegisterAsync(email, "Password123!", "Display", "First", "Last", false, false);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Auth.EmailNotUnique", result.Error.Code);
        
        _userManagerMock.Verify(x => x.CreateAsync(It.IsAny<Account>(), It.IsAny<string>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
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
        var result = await _service.RegisterAsync(email, password, "Display", "First", "Last", false, false);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Auth.RegistrationFailed", result.Error.Code);
        Assert.Contains("Password too short", result.Error.Message);

        _memberRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Member>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
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
        var result = await _service.RegisterAsync(email, password, "Display", "First", "Last", false, false);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Auth.RoleAssignmentFailed", result.Error.Code);
        Assert.Contains("Role assignment failed", result.Error.Message);

        _memberRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Member>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_ShouldRegisterSuccessfully_WhenNoMarketingPreferencesAccepted()
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

        _userManagerMock
            .Setup(x => x.AddToRoleAsync(It.IsAny<Account>(), "User"))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _service.RegisterAsync(email, password, "Display Name", "First Name", "Last Name", false, false);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value);

        _memberRepositoryMock.Verify(x => x.AddAsync(It.Is<Member>(m => m.Email == email), It.IsAny<CancellationToken>()), Times.Once);
        _backgroundJobClientMock.Verify(x => x.Create(It.IsAny<Job>(), It.IsAny<IState>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_ShouldRegisterAndSyncContact_WhenMarketingPreferencesAreAccepted()
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
            .ReturnsAsync(false);

        _roleManagerMock
            .Setup(x => x.CreateAsync(It.IsAny<Role>()))
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock
            .Setup(x => x.AddToRoleAsync(It.IsAny<Account>(), "User"))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _service.RegisterAsync(email, password, "Display Name", "First Name", "Last Name", true, false);

        // Assert
        Assert.True(result.IsSuccess);

        _memberRepositoryMock.Verify(x => x.AddAsync(It.Is<Member>(m => m.Email == email), It.IsAny<CancellationToken>()), Times.Once);
        _backgroundJobClientMock.Verify(x => x.Create(
            It.Is<Job>(job => job.Method.Name == nameof(ICrmSyncJob.SyncContactAsync) &&
                              (Guid)job.Args[0] == result.Value &&
                              (string)job.Args[1] == email &&
                              (string)job.Args[2] == "First Name" &&
                              (string)job.Args[3] == "Last Name" &&
                              (bool)job.Args[4] == true &&
                              (bool)job.Args[5] == false),
            It.IsAny<EnqueuedState>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
