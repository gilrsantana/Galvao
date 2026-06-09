---
name: create-unit-test
description: Write Unit Tests with xUnit, AAA pattern, and Moq to mock interfaces and external dependencies.
---

# Skill: Creating a Unit Test with Moq

This skill guides you through the creation of unit tests utilizing xUnit and the **Moq** library to isolate and verify the behavior of domain entities and application handlers.

---

## Steps

### 1. Identify Target Folder & Class
- Create your test class in the `Blog.UnitTests` project.
- Mirror the file structure of the code under test (e.g., `Application/` or `Domain/`).
- Class name must match the target class with a `Tests` suffix: `{TargetClass}Tests.cs`.

### 2. Set Up Mocks using Moq
- For every interface or external dependency (e.g., repositories, identity services, email services), declare a `Mock<TInterface>` field:
  ```csharp
  private readonly Mock<IUserRepository> _userRepositoryMock = new();
  private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
  ```
- In the constructor of the test class, instantiate the handler, passing the `.Object` of each mock:
  ```csharp
  private readonly UpdateProfileCommandHandler _handler;

  public UpdateProfileCommandHandlerTests()
  {
      _handler = new UpdateProfileCommandHandler(
          _userRepositoryMock.Object, 
          _unitOfWorkMock.Object);
  }
  ```

### 3. Write Test Case using AAA Pattern
- Decorate test methods with xUnit's `[Fact]` attribute.
- Use comments to separate **Arrange**, **Act**, and **Assert** phases.
- Follow the naming pattern: `MethodUnderTest_ShouldExpectedBehavior_WhenCondition`.
- Configure mock responses in the Arrange phase using `.Setup(...)`.
- Assert outcomes using standard xUnit assertions (`Assert.True`, `Assert.Equal`, `Assert.Null`).
- Verify mock interactions in the Assert phase using `.Verify(...)`.

---

## Example Unit Test Class

```csharp
using Moq;
using Blog.Application.UseCases.Users.CommandHandlers;
using Blog.Application.UseCases.Users.Commands;
using Blog.Application.Common.Interfaces;
using Blog.Shared;

namespace Blog.UnitTests.Application;

public class UpdateProfileCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly UpdateProfileCommandHandler _handler;

    public UpdateProfileCommandHandlerTests()
    {
        _handler = new UpdateProfileCommandHandler(
            _userRepositoryMock.Object, 
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnSuccess_WhenCommandIsValid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = User.Create(userId, "john@email.com", "John Doe", "My Bio", "avatar.jpg").Value;
        
        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var command = new UpdateProfileCommand(userId, "John Updated", "New Bio", "new-avatar.jpg");

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("John Updated", user.DisplayName);
        Assert.Equal("New Bio", user.Bio);
        Assert.Equal("new-avatar.jpg", user.AvatarUrl);
        
        // Verify mock behavior
        _userRepositoryMock.Verify(x => x.Update(user), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenUserDoesNotExist()
    {
        // Arrange
        var nonExistentUserId = Guid.NewGuid();
        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(nonExistentUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var command = new UpdateProfileCommand(nonExistentUserId, "John Doe", "Bio", "avatar.jpg");

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("User.NotFound", result.Error.Code);
        
        // Verify update was never called
        _userRepositoryMock.Verify(x => x.Update(It.IsAny<User>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
```
