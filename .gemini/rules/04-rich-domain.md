# Rule: Rich Domain Model and Invariant Encapsulation

## Metadata
- **ID**: RULE-004-RICH-DOMAIN-MODEL
- **Scope**: Blog.Domain
- **Target Types**: Entities, BaseEntity
- **Status**: Active

## Overview
All entities must be rich domain models, meaning state modifications and creation are handled internally via business logic methods. This ensures the domain remains valid and self-encapsulated at all times. All domain entities must inherit from `BaseEntity`.

---

## 1. The `BaseEntity` Abstract Class
Every domain entity must inherit from `BaseEntity`, which standardizes tracking and identification using Guid Version 7 (UUID v7) and creation/update timestamps.

### BaseEntity Implementation:
```csharp
namespace Blog.Domain.Entities;

public abstract class BaseEntity
{
    public Guid Id { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool Active { get; private set; }

    protected BaseEntity()
    {
        Id = Guid.CreateVersion7();
        CreatedAt = DateTime.Now;
        Active = true;
    }

    public virtual void Update()
    {
        UpdatedAt = DateTime.Now;
    }

    public void Activate()
    {
        Active = true;
        Update();
    }

    public void UnActivate()
    {
        Active = false;
        Update();
    }
}
```

---

## 2. Property Encapsulation
- All entity properties must have a **public getter** and a **private setter** (`{ get; private set; }`).
- Never expose public setters. No external code should be able to mutate an entity's properties directly.

---

## 3. Constructor Access Control
- Every entity must have two constructors:
  1. A **parameterless private constructor**: Required for Entity Framework Core materialization and migrations. Explicitly comment this constructor.
  2. A **parameterized private constructor**: Used exclusively by internal static factory methods. This constructor must call the base constructor (`: base()`) to initialize `Id`, `CreatedAt`, and `Active` properties.

---

## 4. Creation via Static Factory Methods
- Entities must **never** be instantiated directly using a `new` operator from the outside.
- Instantiation is done exclusively via a `public static Result<TEntity> Create(...)` factory method.
- The factory method performs validation and returns a `Result<TEntity>` indicating success or failure.

---

## 5. State Mutations
- Modifications to the entity's state must happen through expressive business methods (e.g., `UpdateProfile(...)`, `ChangeAuthor(...)`, `Inactivate()`).
- Methods that mutate state (like updates) must trigger `Update()` to refresh the `UpdatedAt` timestamp.

---

## 6. Rich Domain Entity Template Example

```csharp
using Blog.Shared;

namespace Blog.Domain.Entities;

public class Member : BaseEntity
{
    // 1. Encapsulated Properties
    public string DisplayName { get; private set; }
    public string Email { get; private set; }

    // 2. Private parameterless constructor for EF Core
    private Member() : base()
    {
        DisplayName = string.Empty;
        Email = string.Empty;
    }

    // 2. Private parameterized constructor calling base
    private Member(string email, string displayName) : base()
    {
        Email = email;
        DisplayName = displayName;
    }

    // 3. Static Factory Method
    public static Result<Member> Create(string email, string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            return Result.Failure<Member>(new Error("Member.DisplayNameRequired", "Display name is required."));
        
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            return Result.Failure<Member>(new Error("Member.InvalidEmail", "A valid email is required."));

        return new Member(email, displayName);
    }

    // 4. Mutation Methods
    public Result UpdateProfile(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            return Result.Failure(new Error("Member.DisplayNameRequired", "Display name cannot be empty."));

        DisplayName = displayName;
        Update(); // Trigger base class UpdatedAt update

        return Result.Success();
    }
}
```
