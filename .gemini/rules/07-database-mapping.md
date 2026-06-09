# Rule: Database Configuration, Mapping, and EF Core Patterns

## Metadata
- **ID**: RULE-007-DATABASE-MAPPING
- **Scope**: Blog.Infrastructure
- **Target Types**: DbContext, Entity Configurations, BaseEntityRepository, IUnitOfWork
- **Status**: Active

## Overview
This rule outlines the patterns for database interaction, Entity Framework Core mappings (Fluent API), table naming, foreign keys, and generic repository abstractions implementing Unit of Work.

---

## 1. DbContext Configuration
- **Class Naming**: Use the suffix `DbContext` (e.g., `BlogDbContext`).
- **Identity Integration**: Inherit from `IdentityDbContext<Account, Role, Guid, ...>` for user-auth tables.
- **Model Configuration Discovery**: Auto-discover and apply configurations from the assembly:
  ```csharp
  protected override void OnModelCreating(ModelBuilder builder)
  {
      base.OnModelCreating(builder);
      builder.ApplyConfigurationsFromAssembly(typeof(BlogDbContext).Assembly);
  }
  ```

---

## 2. Fluent API Configuration Patterns
- Define mappings in separate files inside `Persistence/Configurations/`.
- File naming format: `{Entity}Configuration.cs`.
- Implement `IEntityTypeConfiguration<TEntity>`.
- Use explicit table naming, keys, indexes, and column constraints.

---

## 3. Key Mapping Rules
- **Pluralization**: Table names must be pluralized (e.g., `Accounts`, `Posts`, `Users`).
- **Cascade Control**: For all domain relationships, use `DeleteBehavior.Restrict` to prevent accidental deletion cascades. Use `DeleteBehavior.Cascade` only when an entity is physically dependent on another lifecycle-wise (e.g., `User` has a shared primary key with `Account`).
- **Shared Primary Key Pattern**: For `User` entity linked to `Account` authentication:
  ```csharp
  builder.HasKey(u => u.Id);
  builder.HasOne<Account>()
      .WithOne()
      .HasForeignKey<User>(u => u.Id)
      .OnDelete(DeleteBehavior.Cascade);
  ```

---

## 4. Base Repository Pattern (`BaseEntityRepository`)
To guarantee consistency and promote code reuse, all repository implementations must inherit from `BaseEntityRepository<TEntity>`. 

### Rules:
- **Unit of Work**: `BaseEntityRepository<TEntity>` implements `IUnitOfWork`, exposing transactional commit capabilities directly to repositories.
- **Excluded Operations**: **Do NOT implement a delete/remove method.** Logical deletion (inactivation) must be used instead.
- **Standard Methods**:
  - `GetByIdAsync(Guid id)`
  - `AddAsync(TEntity entity)`
  - `Update(TEntity entity)` (must trigger the entity's `Update()` method to refresh the modification timestamp)
  - `GetPagedAsync(int page, int pageSize)` (returns paginated lists using a paged response wrapper)
  - `Activate(TEntity entity)` (calls domain entity activate method)
  - `UnActivate(TEntity entity)` (calls domain entity unactivate method)

### Repository Implementation:
```csharp
using Blog.Domain.Entities;
using Blog.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Blog.Infrastructure.Persistence.Repositories;

public abstract class BaseEntityRepository<TEntity> : IUnitOfWork
    where TEntity : BaseEntity
{
    protected readonly BlogDbContext Context;
    protected readonly DbSet<TEntity> DbSet;

    protected BaseEntityRepository(BlogDbContext context)
    {
        Context = context;
        DbSet = context.Set<TEntity>();
    }

    public async Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await DbSet.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default) =>
        await DbSet.AddAsync(entity, cancellationToken);

    public void Update(TEntity entity)
    {
        entity.Update(); // Updates timestamp
        DbSet.Update(entity);
    }

    public async Task<PagedResponse<TEntity>> GetPagedAsync(
        int page, 
        int pageSize, 
        CancellationToken cancellationToken = default)
    {
        var totalCount = await DbSet.CountAsync(cancellationToken);
        var items = await DbSet
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResponse<TEntity>(items, totalCount, page, pageSize);
    }

    public void Activate(TEntity entity)
    {
        entity.Activate();
        Update(entity);
    }

    public void UnActivate(TEntity entity)
    {
        entity.UnActivate();
        Update(entity);
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        await Context.SaveChangesAsync(cancellationToken);
}
```
