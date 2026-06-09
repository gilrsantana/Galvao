---
name: create-use-case
description: Create a CQRS Use Case (Command/Query, Handler, DTOs, and mappings) and register it in DI.
---

# Skill: Creating a CQRS Use Case

This skill guides you through implementing a Use Case (CQRS pattern) without MediatR.

---

## Steps

### 1. Identify Folders
Organize code in `src/Blog.Application/UseCases/{AggregateName}/`:
- Commands belong in `Commands/` (e.g., `CreateProductCommand.cs`).
- Command Handlers belong in `CommandHandlers/` (e.g., `CreateProductCommandHandler.cs`).
- Queries belong in `Queries/` (e.g., `GetProductByIdQuery.cs`).
- Query Handlers belong in `QueryHandlers/` (e.g., `GetProductByIdQueryHandler.cs`).

### 2. Define Command / Query (Positional Records)
Commands mutate state, queries retrieve data.
- **Example Command** (with return value):
  ```csharp
  using Blog.Application.Common.CQRS;
  using Blog.Shared;

  namespace Blog.Application.UseCases.Products.Commands;

  public record CreateProductCommand(string Name, decimal Price) : ICommand<Guid>;
  ```

- **Example Query**:
  ```csharp
  using Blog.Application.Common.CQRS;
  using Blog.Shared;

  namespace Blog.Application.UseCases.Products.Queries;

  public record GetProductByIdQuery(Guid ProductId) : IQuery<ProductResponse>;
  ```

### 3. Create the Response/DTO
- Place response records in `Queries/` or adjacent folders as positional `record` types:
  ```csharp
  namespace Blog.Application.UseCases.Products.Queries;

  public record ProductResponse(Guid Id, string Name, decimal Price, DateTime CreatedAt);
  ```

### 4. Create the Handler
- Create class implementing `ICommandHandler<TCommand>` or `ICommandHandler<TCommand, TResponse>` or `IQueryHandler<TQuery, TResponse>`.
- Injected dependencies must use private readonly fields with an underscore prefix (`_productRepository`).
- Inside the method:
  1. Fetch data / execute validations. Return `Result.Failure` early on failure.
  2. Perform mutations using domain entity methods.
  3. Call `_unitOfWork.SaveChangesAsync(cancellationToken)` on mutations.
  4. Perform manual mapping (no AutoMapper/Mapperly) to DTOs or response types.
  5. Return `Result.Success(...)`.

- **Example Handler**:
  ```csharp
  using Blog.Application.Common.CQRS;
  using Blog.Application.Common.Interfaces;
  using Blog.Application.UseCases.Products.Commands;
  using Blog.Shared;

  namespace Blog.Application.UseCases.Products.CommandHandlers;

  public class CreateProductCommandHandler : ICommandHandler<CreateProductCommand, Guid>
  {
      private readonly IProductRepository _productRepository;
      private readonly IUnitOfWork _unitOfWork;

      public CreateProductCommandHandler(IProductRepository productRepository, IUnitOfWork unitOfWork)
      {
          _productRepository = productRepository;
          _unitOfWork = unitOfWork;
      }

      public async Task<Result<Guid>> HandleAsync(CreateProductCommand command, CancellationToken cancellationToken = default)
      {
          var existingProduct = await _productRepository.GetByNameAsync(command.Name, cancellationToken);
          if (existingProduct is not null)
              return Result.Failure<Guid>(new Error("Product.NameNotUnique", "Product name must be unique."));

          var productResult = Product.Create(command.Name, command.Price);
          if (productResult.IsFailure)
              return Result.Failure<Guid>(productResult.Error);

          await _productRepository.AddAsync(productResult.Value, cancellationToken);
          await _unitOfWork.SaveChangesAsync(cancellationToken);

          return productResult.Value.Id; // Relies on implicit conversion to Result<Guid>
      }
  }
  ```

### 5. Explicit DI Registration
- Open `src/Blog.Application/Extensions/DependencyInjection.cs`.
- Add your handler registration to the `AddApplication` method:
  ```csharp
  // Commands
  services.AddScoped<ICommandHandler<CreateProductCommand, Guid>, CreateProductCommandHandler>();
  ```
