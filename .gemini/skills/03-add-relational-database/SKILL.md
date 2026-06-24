---
name: add-relational-database
description: Add and configure the MySQL database in the project using EF Core, DatabaseOptions, and Migrations.
---

# Skill: Configuring MySQL Relational Database with EF Core

This skill guides you through configuring the MySQL database provider using EF Core Code-First patterns tailored to the **Galvao** project.

---

## Configuration Steps

### 1. Verification of NuGet Packages
Ensure the MySQL EF Core provider is installed in the `src/Galvao.Infrastructure` project:
- Package: `MySql.EntityFrameworkCore`

If not installed, install it:
`dotnet add src/Galvao.Infrastructure package MySql.EntityFrameworkCore`

---

### 2. Configure DatabaseOptions POCO
The `DatabaseOptions` class resides in `src/Galvao.Infrastructure/Configurations/DatabaseOptions.cs`:
```csharp
namespace Galvao.Infrastructure.Configurations;

public class DatabaseOptions
{
    public const string SectionName = "DatabaseOptions";

    public bool EnableDetailedErrors { get; set; } = false;
    public bool EnableSensitiveDataLogging { get; set; } = false;
    public int CommandTimeout { get; set; } = 30;
    public bool EnableRetryOnFailure { get; set; } = false;
    public int MaxRetryCount { get; set; } = 3;
    public int MaxRetryDelaySeconds { get; set; } = 5;
}
```

---

### 3. Register MySQL DbContext in Dependency Injection
Update `src/Galvao.Infrastructure/Extensions/DependencyInjection.cs` to set up [GalvaoDbContext](file:///home/gilmar/Development/ai-driven-development/projects/galvao/backend/src/Galvao.Infrastructure/Persistence/GalvaoDbContext.cs#L13):

```csharp
using Galvao.Infrastructure.Configurations;
using Galvao.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
{
    var connectionString = configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrEmpty(connectionString))
        throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

    services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionName));
    var dbOptions = configuration.GetSection(DatabaseOptions.SectionName).Get<DatabaseOptions>() ?? new DatabaseOptions();

    services.AddDbContext<GalvaoDbContext>(options =>
    {
        options.UseMySQL(connectionString, mysqlOptions =>
        {
            mysqlOptions.CommandTimeout(dbOptions.CommandTimeout);
            // MySql.EntityFrameworkCore doesn't always support EnableRetryOnFailure in the same way as Pomelo,
            // check configuration and capabilities of the current driver.
        });

        if (dbOptions.EnableDetailedErrors)
            options.EnableDetailedErrors();
        if (dbOptions.EnableSensitiveDataLogging)
            options.EnableSensitiveDataLogging();
    });

    return services;
}
```

---

### 4. Update Application Settings
Ensure `appsettings.json` has:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=galvao;User=root;Password=yourpassword;"
  },
  "DatabaseOptions": {
    "EnableDetailedErrors": true,
    "EnableSensitiveDataLogging": true,
    "CommandTimeout": 30,
    "EnableRetryOnFailure": false,
    "MaxRetryCount": 3,
    "MaxRetryDelaySeconds": 5
  }
}
```
