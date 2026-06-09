# Rule: Custom CQRS (Command Query Responsibility Segregation)

## Metadata
- **ID**: RULE-005-CUSTOM-CQRS
- **Scope**: Blog.Application & Blog.Presentation
- **Target Types**: Commands, Queries, Handlers
- **Status**: Active

## Overview
This project uses a custom hand-rolled CQRS implementation. It **does not** use MediatR or any third-party mediator frameworks. Use Case orchestration is managed via direct Dependency Injection of specialized handlers.

---

## 1. Marker Interfaces (`Blog.Application/Common/CQRS/`)

### Commands
- Write operations that mutate state.
- **ICommand**: Used for commands that do not return a value (they return a simple `Result`).
- **ICommand\<TResponse>**: Used for commands that return a value on success (e.g., entity IDs, authentication tokens).
```csharp
public interface ICommand { }
public interface ICommand<TResponse> { }
```

### Queries
- Read operations that fetch data.
- **IQuery\<TResponse>**: Always returns a value.
```csharp
public interface IQuery<TResponse> { }
```

---

## 2. Handler Interfaces

### Command Handlers
```csharp
public interface ICommandHandler<in TCommand>
    where TCommand : ICommand
{
    Task<Result> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}

public interface ICommandHandler<in TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    Task<Result<TResponse>> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}
```

### Query Handlers
```csharp
public interface IQueryHandler<in TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    Task<Result<TResponse>> HandleAsync(TQuery query, CancellationToken cancellationToken = default);
}
```

---

## 3. Implementation Rules
- **Commands and Queries**: Define them as positional immutable `record` types.
- **Handlers**: Define them as regular classes. Keep handler code focused on a single Use Case.
- **Method Naming**: The execution method must always be named **`HandleAsync`** and return a `Task<Result>` or `Task<Result<TResponse>>`.
- **Dependency Injection**: Dependencies must be injected via the constructor as `private readonly` fields with an underscore prefix (`_repository`).

---

## 4. Example Use Case Implementation

### The Command:
```csharp
namespace Blog.Application.UseCases.Posts.Commands;

public record ChangePostAuthorCommand(Guid PostId, Guid NewAuthorId) : ICommand;
```

### The Handler:
```csharp
using Blog.Application.Common.CQRS;
using Blog.Application.Common.Interfaces;
using Blog.Shared;

namespace Blog.Application.UseCases.Posts.CommandHandlers;

public class ChangePostAuthorCommandHandler : ICommandHandler<ChangePostAuthorCommand>
{
    private readonly IPostRepository _postRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ChangePostAuthorCommandHandler(
        IPostRepository postRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
    {
        _postRepository = postRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> HandleAsync(ChangePostAuthorCommand command, CancellationToken cancellationToken = default)
    {
        var post = await _postRepository.GetByIdAsync(command.PostId, cancellationToken);
        if (post is null)
            return Result.Failure(new Error("Post.NotFound", "Post not found."));

        var authorExists = await _userRepository.GetByIdAsync(command.NewAuthorId, cancellationToken);
        if (authorExists is null)
            return Result.Failure(new Error("User.NotFound", "New Author not found."));

        var result = post.ChangeAuthor(command.NewAuthorId);
        if (result.IsFailure)
            return Result.Failure(result.Error);

        _postRepository.Update(post);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
```

---

## 5. Dependency Injection Registration
Every handler must be registered explicitly as `Scoped` inside `Blog.Application/Extensions/DependencyInjection.cs`:
```csharp
services.AddScoped<ICommandHandler<ChangePostAuthorCommand>, ChangePostAuthorCommandHandler>();
```
*(No assembly scanning is used; registration must be manual and explicit).*
