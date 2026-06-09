# Rule: Result Pattern and Business Error Handling

## Metadata
- **ID**: RULE-003-RESULT-ERROR-HANDLING
- **Scope**: Entire Solution
- **Target Types**: Result, Error, ValidationError, Handlers, Controllers
- **Status**: Active

## Overview
This codebase avoids raising exceptions for expected business errors (e.g., entity validation failed, record not found, authorization denied). Instead, it uses a functional **Result** pattern defined in `Blog.Shared` to represent outcomes explicitly. Exceptions are reserved strictly for exceptional runtime failures (database connection drop, system crash).

---

## 1. Shared Error and Result Primitives (`Blog.Shared`)

All projects in the solution (`Blog.Domain`, `Blog.Application`, `Blog.Infrastructure`, `Blog.Presentation`) must reference the `Blog.Shared` project:

```xml
<ItemGroup>
  <ProjectReference Include="..\Blog.Shared\Blog.Shared.csproj" />
</ItemGroup>
```

And import the namespace at the top of C# files:
```csharp
using Blog.Shared;
```

### A. The `Error` Record
An immutable record representing a general application error:
```csharp
namespace Blog.Shared;

public record Error(string Code, string Message)
{
    public static readonly Error None = new(string.Empty, string.Empty);
}
```

### B. The `ValidationError` Record
A specialized record extending `Error` to represent property-specific validation failures. It carries a fixed code of `"Validation.Error"` and defines a `PropertyName` property for field-level identification:
```csharp
namespace Blog.Shared;

public record ValidationError(string PropertyName, string ErrorMessage) 
    : Error("Validation.Error", ErrorMessage);
```

### C. The `Result` and `Result<TValue>` Classes
Wrappers representing the outcome of an operation. 
- A successful result has `IsSuccess = true` and `Error = Error.None`.
- A failed result has `IsSuccess = false` and `Error` populated with a specific error/validation error.
- Generic `Result<TValue>` exposes a `Value` property (which throws an `InvalidOperationException` if accessed on a failed result) and supports implicit conversion from `TValue`.

```csharp
namespace Blog.Shared;

public class Result
{
    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
            throw new InvalidOperationException("A successful result cannot have an error.");
        if (!isSuccess && error == Error.None)
            throw new InvalidOperationException("A failed result must have an error.");

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }

    public static Result Success() => new(true, Error.None);
    public static Result Failure(Error error) => new(false, error);
    public static Result<TValue> Success<TValue>(TValue value) => new(value, true, Error.None);
    public static Result<TValue> Failure<TValue>(Error error) => new(default!, false, error);
}

public class Result<TValue> : Result
{
    private readonly TValue _value;

    protected internal Result(TValue value, bool isSuccess, Error error)
        : base(isSuccess, error)
    {
        _value = value;
    }

    public TValue Value => IsSuccess
        ? _value
        : throw new InvalidOperationException("The value of a failure result cannot be accessed.");

    public static implicit operator Result<TValue>(TValue value) => Success(value);
}
```

---

## 2. Error Code Conventions
All error codes must follow the dot notation: `Domain.ErrorName` (e.g., `Post.TitleRequired`, `User.EmailNotUnique`, `Auth.InvalidCredentials`).
- **404 Not Found mapping**: Triggered if the error code contains the word `"NotFound"`.
- **400 Bad Request mapping**: Triggered if the error code contains the word `"Required"`, `"Invalid"`, or `"NotUnique"`.
- **401 Unauthorized mapping**: Triggered if the error code is `"Auth.InvalidCredentials"`, `"Auth.InvalidToken"`, `"Token.Expired"`, or `"Auth.InvalidRefreshToken"`.

---

## 3. Propagation in Domain and Handlers

### In Domain Entities (Static Factories / Mutation):
```csharp
public static Result<Post> Create(string title, string slug, Guid authorId)
{
    if (string.IsNullOrWhiteSpace(title))
        return Result.Failure<Post>(new Error("Post.TitleRequired", "Title is required."));

    return new Post(Guid.NewGuid(), title, slug, authorId);
}
```

### In Application Handlers:
```csharp
public async Task<Result<Guid>> HandleAsync(CreatePostCommand command, CancellationToken cancellationToken = default)
{
    var postResult = Post.Create(command.Title, command.Slug, command.AuthorId);
    if (postResult.IsFailure)
        return Result.Failure<Guid>(postResult.Error); // Propagate failed result

    await _postRepository.AddAsync(postResult.Value, cancellationToken);
    await _unitOfWork.SaveChangesAsync(cancellationToken);

    return postResult.Value.Id; // Implicit conversion to Result<Guid>
}
```

---

## 4. Presentation Mapping to HTTP Status
Controllers inherit from `ApiControllerBase` which evaluates `Result` or `Result<T>` and returns `Ok()`, `Ok(value)`, or a formatted RFC 7807 `ProblemDetails` response:

```csharp
protected IActionResult HandleResult(Result result)
{
    if (result.IsSuccess)
        return Ok();

    return MapFailureResult(result);
}
```
*(Never manually return BadRequest/NotFound in controller endpoints; always delegate to `HandleResult(result)`)*
