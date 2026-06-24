---
name: database-migrations-mysql
description: Manage Entity Framework Core migrations and database seeding for MySQL inside the Galvao project workspace.
---

# Skill: Managing MySQL Entity Framework Migrations & Seed Data

This skill details the operational commands and standards for updating the MySQL database schema and populating initial seed records.

---

## 1. Migration Command CLI Execution
Always run the commands from the `/backend/` root folder of the workspace.

- **Add Migration**:
  Use explicit PascalCase naming without special characters (e.g. `AddCategoriesTable`, `ModifyMemberContactUniqueKey`).
  ```bash
  dotnet ef migrations add <MigrationName> --project src/Galvao.Infrastructure --startup-project src/Galvao.Presentation
  ```

- **Remove Last Migration** (only if it has not been applied to a running database yet):
  ```bash
  dotnet ef migrations remove --project src/Galvao.Infrastructure --startup-project src/Galvao.Presentation
  ```

- **Update Database Locally**:
  ```bash
  dotnet ef database update --project src/Galvao.Infrastructure --startup-project src/Galvao.Presentation
  ```

- **Generate SQL Script** (for review or production deployment):
  ```bash
  dotnet ef migrations script --project src/Galvao.Infrastructure --startup-project src/Galvao.Presentation -o migrations.sql
  ```

---

## 2. Reviewing Migrations
Before applying or committing a migration:
- Inspect the generated C# files inside `src/Galvao.Infrastructure/Migrations/`.
- Ensure MySQL syntax translations are correct (e.g., column lengths, indexes, constraints).
- Confirm that foreign keys are configured with `onDelete: ReferentialAction.Restrict` unless cascading deletion is explicitly desired for dependent items.

---

## 3. Database Seeding Strategy
To initialize reference data (e.g., standard roles, permissions, administrative settings):

### A. Fluent API Seed Data (Model-level)
For immutable reference metadata, define it directly inside the `OnModelCreating` configuration of [GalvaoDbContext](file:///home/gilmar/Development/ai-driven-development/projects/galvao/backend/src/Galvao.Infrastructure/Persistence/GalvaoDbContext.cs#L38) using `builder.Entity<T>().HasData(...)`.
```csharp
public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles");
        builder.HasData(
            Role.Create("Admin", "Full administrative clearance."),
            Role.Create("Member", "Standard authenticated user account.")
        );
    }
}
```

### B. Startup Runtime Seeding (Operational-level)
For mutable entities or complex operational data seeding (e.g. initial showroom items):
- Avoid putting large mutable datasets in EF migrations.
- Instead, implement a dedicated database initializer service called during application bootstrapping inside `Program.cs`.
