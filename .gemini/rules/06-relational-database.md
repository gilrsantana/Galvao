# Rule: Relational Database Configuration and Migration Lifecycle

## Metadata
- **ID**: RULE-06-RELATIONAL-DATABASE
- **Scope**: Blog.Infrastructure & appsettings.json
- **Target Types**: DbContext, DatabaseOptions, EF Core Mappings
- **Status**: Active

## Overview
All relational database integrations must follow a strict Code-First approach, separating operational runtime options (timeout, retries, logging) into a dedicated `DatabaseOptions` POCO and mapping database entities with robust relational rules (no cascade deletes on domain models).

---

## 1. The Options Pattern for Database Configuration
- Operational database options must be defined in a POCO class named `DatabaseOptions` inside the `Configurations/` folder of the Infrastructure project.
- It must contain a `const string SectionName = "DatabaseOptions";` representing its configuration key.
- It must model:
  - Logging & detail options: `EnableDetailedErrors` (bool), `EnableSensitiveDataLogging` (bool).
  - Performance & Resiliency options: `CommandTimeout` (int), `EnableRetryOnFailure` (bool), `MaxRetryCount` (int), `MaxRetryDelaySeconds` (int), `MaxBatchSize` (int?).

---

## 2. Options Registration and Context Configuration
- Database options must be registered and bound inside the Infrastructure layer's DI class using:
  ```csharp
  services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionName));
  ```
- The DbContext is configured using these options to enable connection resiliency and logging.
- Check for connection string existence early:
  ```csharp
  var connectionString = configuration.GetConnectionString("DefaultConnection");
  if (string.IsNullOrEmpty(connectionString))
      throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
  ```

---

## 3. Code-First Migration Lifecycle
- All database schemas must be modeled code-first.
- Always apply migrations using:
  - EF CLI tool: `dotnet ef migrations add <MigrationName> --project src/Blog.Infrastructure --startup-project src/Blog.Presentation`
- Do not make direct alterations to the database schema. All changes must go through Migrations.

---

## 4. Entity Mapping Standards
- Implement `IEntityTypeConfiguration<T>` in separate files inside `Persistence/Configurations/` format.
- **Cascade Control**: Set `DeleteBehavior.Restrict` for all foreign key mappings on domain boundaries.
- Example mapping rule:
  ```csharp
  builder.HasOne<User>()
      .WithMany()
      .HasForeignKey(x => x.UserId)
      .OnDelete(DeleteBehavior.Restrict);
  ```
