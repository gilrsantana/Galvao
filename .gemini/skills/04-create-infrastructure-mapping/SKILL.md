---
name: create-infrastructure-mapping
description: Implement Entity Framework Core mapping, and create repository classes inheriting from BaseEntityRepository.
---

# Skill: Creating Infrastructure Mapping and Repositories

This skill guides you through EF Core database mappings and repository implementations using `BaseEntityRepository`.

---

## Steps

### 1. Define Entity Configuration
- Create a file inside `src/Blog.Infrastructure/Persistence/Configurations/` named `{EntityName}Configuration.cs`.
- Implement `IEntityTypeConfiguration<TEntity>`.
- Use the Fluent API to map the entity properties, keys, and indexes.
- Explicitly configure foreign key mappings with `OnDelete(DeleteBehavior.Restrict)`.

---

### 2. Implement the Repository
- Create a class inside `src/Blog.Infrastructure/Persistence/Repositories/` named `{EntityName}Repository.cs`.
- Inherit from `BaseEntityRepository<{EntityName}>`.
- Implement the interface defined in `Blog.Application/Common/Interfaces/` (e.g., `IProductRepository`).
- Constructor-inject `BlogDbContext` and pass it to the base constructor using `: base(context)`.
- **Inherited Methods**: Your repository automatically inherits standard CRUD operations:
  - `GetByIdAsync(...)`
  - `AddAsync(...)`
  - `Update(...)` (triggers modification timestamps automatically)
  - `GetPagedAsync(...)` (paginated results)
  - `Activate(...)`
  - `UnActivate(...)`
  - `SaveChangesAsync(...)` (Unit of Work commit)
- **Rules**: Do not implement any `Delete` or `Remove` methods. Deletion must be logical using `UnActivate()`.

- **Example Repository**:
  ```csharp
  using Blog.Domain.Entities;
  using Blog.Application.Common.Interfaces;
  using Blog.Infrastructure.Persistence;
  using Microsoft.EntityFrameworkCore;

  namespace Blog.Infrastructure.Persistence.Repositories;

  public class ProductRepository : BaseEntityRepository<Product>, IProductRepository
  {
      public ProductRepository(BlogDbContext context) : base(context)
      {
      }

      // Add specific read methods not covered by base repository
      public async Task<Product?> GetByNameAsync(string name, CancellationToken cancellationToken = default) =>
          await DbSet.FirstOrDefaultAsync(p => p.Name == name, cancellationToken);
  }
  ```

---

### 3. Dependency Injection Registration
- Open `src/Blog.Infrastructure/Extensions/DependencyInjection.cs`.
- Register your repository:
  ```csharp
  services.AddScoped<IProductRepository, ProductRepository>();
  ```
