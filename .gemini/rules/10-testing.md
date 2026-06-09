# Rule: Testing Conventions and Best Practices

## Metadata
- **ID**: RULE-010-TESTING
- **Scope**: Blog.UnitTests & Blog.IntegrationTests
- **Target Types**: Test Classes, Fakes, Fixtures, Mocks
- **Status**: Active

## Overview
Quality and test coverage are verified via xUnit test suites. We favor pure, fast unit tests using the **Moq** mocking library to isolate dependencies, and E2E integration tests against the actual database.

---

## 1. Test Organization & Naming
- **Structure**: Tests mirror the layout of source layers (e.g., `Domain/`, `Application/`, `Shared/`).
- **Naming format**: Test classes must end with `Tests` (e.g., `PostTests`, `CreatePostCommandHandlerTests`).
- **Method Naming**: Always use the triple-section pattern:
  `MethodUnderTest_ShouldExpectedBehavior_WhenCondition`
  - Example: `Create_ShouldReturnFailure_WhenTitleIsEmpty`
  - Example: `Handle_ShouldReturnSuccess_WhenCommandIsValid`

---

## 2. Unit Testing and Mocking Style
- **Mocking Framework**: Use the **Moq** library to mock interfaces and external dependencies (repositories, unit of work, authentication, email delivery).
- **Constructors Setup**: Declare class mock fields as `Mock<TInterface>` and instantiate target components using `mock.Object`.
- **Assertions & Verification**: Use standard xUnit assertions (`Assert.True`, `Assert.Equal`, `Assert.Single`) combined with Moq's `.Verify(...)` to check execution frequencies. Do not use FluentAssertions or Shouldly.

### Example Unit Test:
```csharp
using Moq;
using Blog.Application.UseCases.Posts.CommandHandlers;
using Blog.Application.UseCases.Posts.Commands;
using Blog.Application.Common.Interfaces;
using Blog.Shared;

namespace Blog.UnitTests.Application;

public class CreatePostCommandHandlerTests
{
    private readonly Mock<IPostRepository> _postRepositoryMock = new();
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly CreatePostCommandHandler _handler;

    public CreatePostCommandHandlerTests()
    {
        _handler = new CreatePostCommandHandler(
            _postRepositoryMock.Object, 
            _userRepositoryMock.Object, 
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnSuccess_WhenCommandIsValid()
    {
        // Arrange
        var authorId = Guid.NewGuid();
        var command = new CreatePostCommand(
            "Valid Title", 
            "valid-slug", 
            "Summary text", 
            "Full content text", 
            "tag1, tag2", 
            "image.jpg", 
            authorId);

        _postRepositoryMock
            .Setup(x => x.GetBySlugAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Post?)null);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value);
        
        // Verify mock actions
        _postRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Post>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
```

---

## 3. Integration Testing Style
- **Real Database Connectivity**: Do not use in-memory databases (like InMemoryProvider or SQLite). Integration tests must run against the real production-like database (configured via connection strings in User Secrets / Appsettings).
- **WebApplicationFactory**: Inherit from `WebApplicationFactory<Program>` to launch the complete system pipeline.
- **Test Lifecycle**: Share the server fixture across test runs using xUnit's `IClassFixture<CustomWebApplicationFactory>`.
- **E2E Flow**: Focus on verifying the complete operational flow inside a single test (e.g., Register → Login → Create Post → Delete Post) to check transactional state.
- **Cleanup**: Programmatically cleanup and delete created resources (such as test users or database records) at the end of the test execution.
