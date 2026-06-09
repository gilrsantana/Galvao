---
name: add-relational-database
description: Add and configure a relational database (PostgreSQL, SQL Server, MySQL, SQLite, Oracle) in the project using EF Core, DatabaseOptions, and Migrations.
---

# Skill: Adding a Relational Database with EF Core

This skill guides the assistant through adding, configuring, and registering a relational database provider using EF Core Code-First patterns.

---

## Interactive Initiation (REQUIRED STEP)
Before executing any file modifications or package installations, the assistant **MUST** prompt the user to get:
1. The target **Relational Database Provider** (PostgreSQL, SQL Server, MySQL, SQLite, Oracle).
2. The **Connection String** for the database.

*Example query to the user:*
> Please specify:
> 1. Which database engine should be used? (PostgreSQL, MS SQL Server, MySQL, SQLite, Oracle)
> 2. What is the database connection string?

---

## Configuration Steps

### 1. Install Required EF Core Packages
Based on the selected provider, install the appropriate NuGet package inside the `src/Blog.Infrastructure` project:

- **PostgreSQL**:
  `dotnet add src/Blog.Infrastructure package Npgsql.EntityFrameworkCore.PostgreSQL`
- **SQL Server**:
  `dotnet add src/Blog.Infrastructure package Microsoft.EntityFrameworkCore.SqlServer`
- **MySQL**:
  `dotnet add src/Blog.Infrastructure package MySql.EntityFrameworkCore`
- **SQLite**:
  `dotnet add src/Blog.Infrastructure package Microsoft.EntityFrameworkCore.Sqlite`
- **Oracle**:
  `dotnet add src/Blog.Infrastructure package Oracle.EntityFrameworkCore`

### 2. Create the DatabaseOptions POCO
Create `src/Blog.Infrastructure/Configurations/DatabaseOptions.cs` to hold database parameters:
```csharp
namespace Blog.Infrastructure.Configurations;

public class DatabaseOptions
{
    public const string SectionName = "DatabaseOptions";

    public bool EnableDetailedErrors { get; set; } = false;
    public bool EnableSensitiveDataLogging { get; set; } = false;
    public int CommandTimeout { get; set; } = 30;
    public bool EnableRetryOnFailure { get; set; } = true;
    public int MaxRetryCount { get; set; } = 3;
    public int MaxRetryDelaySeconds { get; set; } = 5;
    public int? MaxBatchSize { get; set; }
}
```

### 3. Register Database in Dependency Injection
Update `src/Blog.Infrastructure/Extensions/DependencyInjection.cs`:
- Retrieve and bind `DatabaseOptions` from configuration.
- Add and configure the DbContext with the selected database provider.

- **Example Setup (PostgreSQL)**:
  ```csharp
  var connectionString = configuration.GetConnectionString("DefaultConnection");
  if (string.IsNullOrEmpty(connectionString))
      throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

  services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionName));
  var dbOptions = configuration.GetSection(DatabaseOptions.SectionName).Get<DatabaseOptions>() ?? new DatabaseOptions();

  services.AddDbContext<BlogDbContext>(options =>
  {
      options.UseNpgsql(connectionString, npgsqlOptions =>
      {
          npgsqlOptions.CommandTimeout(dbOptions.CommandTimeout);
          if (dbOptions.EnableRetryOnFailure)
          {
              npgsqlOptions.EnableRetryOnFailure(
                  maxRetryCount: dbOptions.MaxRetryCount,
                  maxRetryDelay: TimeSpan.FromSeconds(dbOptions.MaxRetryDelaySeconds),
                  errorCodesToAdd: null);
          }
          if (dbOptions.MaxBatchSize.HasValue)
          {
              npgsqlOptions.MaxBatchSize(dbOptions.MaxBatchSize.Value);
          }
      });

      if (dbOptions.EnableDetailedErrors)
          options.EnableDetailedErrors();
      if (dbOptions.EnableSensitiveDataLogging)
          options.EnableSensitiveDataLogging();
  });
  ```

### 4. Update Application Settings
Add configuration entries to `appsettings.json` and `appsettings.Development.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "YOUR_CONNECTION_STRING_HERE"
  },
  "DatabaseOptions": {
    "EnableDetailedErrors": true,
    "EnableSensitiveDataLogging": true,
    "CommandTimeout": 30,
    "EnableRetryOnFailure": true,
    "MaxRetryCount": 3,
    "MaxRetryDelaySeconds": 5
  }
}
```
