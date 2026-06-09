---
name: create-api-controller
description: Create an API Controller that inherits from ApiControllerBase, uses constructor injection for CQRS handlers, maps outcomes using HandleResult, and includes OpenAPI annotations.
---

# Skill: Creating an API Controller

This skill guides you through implementing HTTP controllers in the Presentation layer, utilizing the project's standard response mapping and OpenAPI/Scalar metadata documentation.

---

## Steps

### 1. File Location & Namespace
- Create a file inside `src/Blog.Presentation/Controllers/` named `{Name}sController.cs` (e.g., `ProductsController.cs`).
- Namespace must be `Blog.Presentation.Controllers`.

### 2. Base Configuration
- Inherit from `ApiControllerBase`.
- Add `[AllowAnonymous]` to individual endpoints if the controller class-level `[Authorize]` constraint should be bypassed.

### 3. Constructor Injection
- Injects CQRS handlers (`ICommandHandler<TCommand>` / `IQueryHandler<TQuery, TResponse>`) as `private readonly` fields. Do not inject repositories or database contexts directly.

### 4. Implement Endpoint Actions with OpenAPI Annotations
- Every endpoint action must return `Task<IActionResult>`.
- Build commands or queries using request parameters.
- Execute handlers using `await _handler.HandleAsync(command/query, cancellationToken)`.
- **OpenAPI Metadata**: Decorate actions with `[ProducesResponseType]` attributes to specify possible HTTP response statuses and DTO schemas for Scalar API generation.
- Map results using `HandleResult(result)` or `HandleResult<T>(result)`.

---

## Code Template

```csharp
using Blog.Application.Common.CQRS;
using Blog.Application.UseCases.Products.Commands;
using Blog.Application.UseCases.Products.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Blog.Presentation.Controllers;

public class ProductsController : ApiControllerBase
{
    private readonly ICommandHandler<CreateProductCommand, Guid> _createHandler;
    private readonly IQueryHandler<GetProductByIdQuery, ProductResponse> _getByIdHandler;

    public ProductsController(
        ICommandHandler<CreateProductCommand, Guid> createHandler,
        IQueryHandler<GetProductByIdQuery, ProductResponse> getByIdHandler)
    {
        _createHandler = createHandler;
        _getByIdHandler = getByIdHandler;
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetProductByIdQuery(id);
        var result = await _getByIdHandler.HandleAsync(query, cancellationToken);
        
        return HandleResult(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create([FromBody] CreateProductCommand command, CancellationToken cancellationToken)
    {
        var result = await _createHandler.HandleAsync(command, cancellationToken);
        
        if (result.IsFailure)
            return HandleResult(result);

        return CreatedAtAction(nameof(GetById), new { id = result.Value }, result.Value);
    }
}
```
