---
name: create-domain-entity
description: Create a rich domain model C# entity inheriting from BaseEntity, using private constructors, static factory creation methods, and UUID v7.
---

# Skill: Creating a Rich Domain Entity

This skill guides you through the creation of a domain entity following the project's strict Rich Domain Model and `BaseEntity` standards.

---

## Steps

### 1. File Location & Namespace
- Create a file inside `src/Blog.Domain/Entities/` named `{EntityName}.cs`.
- Namespace must be `Blog.Domain.Entities`.

### 2. Base Class Inheritance
- All domain entities must inherit from `BaseEntity`.
- Verify that the `BaseEntity` abstract class is present in the `Entities/` folder containing the following properties:
  - `Id` (`Guid` initialized with `Guid.CreateVersion7()`).
  - `CreatedAt` (`DateTime` set to `DateTime.Now`).
  - `UpdatedAt` (`DateTime?` nullable).
  - `Active` (`bool` set to `true`).
  - Methods: `Update()` (virtual), `Activate()`, and `UnActivate()`.

### 3. Properties Encapsulation
- Define all properties with public getters and private setters:
  ```csharp
  public string Title { get; private set; }
  ```
- Do not expose setters publicly.

### 4. Dual Constructor Pattern
- Provide a `private` parameterless constructor for EF Core materialization, calling `: base()`.
- Provide a `private` parameterized constructor to assign values, calling `: base()`.

### 5. Static Factory Method
- Create a public static factory method returning `Result<{EntityName}>`.
- Validate invariants using guard clauses and return `Result.Failure<{EntityName}>(new Error(...))` on error.

### 6. Mutation Methods
- Create methods for updates (e.g., `UpdateDetails(...)`).
- Inside update methods, always call `Update()` (inherited from `BaseEntity`) to refresh the `UpdatedAt` timestamp.

---

## Code Template

```csharp
using Blog.Shared;

namespace Blog.Domain.Entities;

public class Product : BaseEntity
{
    // Properties
    public string Name { get; private set; }
    public decimal Price { get; private set; }

    // EF Core Constructor
    private Product() : base()
    {
        Name = string.Empty;
    }

    // Full Constructor
    private Product(string name, decimal price) : base()
    {
        Name = name;
        Price = price;
    }

    // Static Factory
    public static Result<Product> Create(string name, decimal price)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<Product>(new Error("Product.NameRequired", "Product name is required."));

        if (price <= 0)
            return Result.Failure<Product>(new Error("Product.InvalidPrice", "Price must be greater than zero."));

        return new Product(name, price);
    }

    // Mutation Method
    public Result UpdateDetails(string name, decimal price)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure(new Error("Product.NameRequired", "Product name cannot be empty."));

        if (price <= 0)
            return Result.Failure(new Error("Product.InvalidPrice", "Price must be greater than zero."));

        Name = name;
        Price = price;
        
        Update(); // Trigger BaseEntity UpdatedAt update
        
        return Result.Success();
    }
}
```
