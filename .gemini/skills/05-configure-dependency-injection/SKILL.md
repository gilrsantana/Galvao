---
name: configure-dependency-injection
description: Configure modular dependency injection classes, register services in Extensions, build the Composition Root, and maintain a clean Program.cs.
---

# Skill: Configuring Dependency Injection and Orchestration

This skill guides you through implementing modular dependency injection (DI) and Composition Root settings for C# solutions.

---

## Steps

### 1. Set Up Layer Extensions
For any new project layer (e.g., Application, Infrastructure, or a new module):
- Create an `Extensions/` directory inside the project.
- Create a `DependencyInjection.cs` static class.
- Expose an extension method to register services onto `IServiceCollection`.

- **Template for Application Layer**:
  ```csharp
  using Microsoft.Extensions.DependencyInjection;
  using Blog.Application.Common.CQRS;

  namespace Blog.Application.Extensions;

  public static class DependencyInjection
  {
      public static IServiceCollection AddApplication(this IServiceCollection services)
      {
          // Register handlers, validators, services
          return services;
      }
  }
  ```

- **Template for Infrastructure Layer**:
  ```csharp
  using Microsoft.Extensions.DependencyInjection;
  using Microsoft.Extensions.Configuration;

  namespace Blog.Infrastructure.Extensions;

  public static class DependencyInjection
  {
      public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
      {
          // Register DB Contexts, Options, Repositories, External Services
          return services;
      }
  }
  ```

### 2. Configure the Composition Root (Presentation Layer)
- Inside the Presentation project, navigate to `Configurations/`.
- Open `DependencyInjection.cs`.
- Inside `AddPresentationServices`, configure API services (CORS, Auth, Swagger/Scalar) and chain-register the other layers:
  ```csharp
  public static IServiceCollection AddPresentationServices(this IServiceCollection services, IConfiguration configuration)
  {
      services.AddControllers();
      // Auth, CORS, Scalar configs...

      // Chain Registrations
      services.AddApplication()
              .AddInfrastructure(configuration);

      return services;
  }
  ```
- Inside the `Configure` method, define the middleware pipeline sequentially:
  ```csharp
  public static void Configure(this WebApplication app)
  {
      app.UseMiddleware<CustomExceptionHandlingMiddleware>();
      
      if (app.Environment.IsDevelopment())
      {
          app.MapOpenApi();
          app.MapScalarApiReference();
      }

      app.UseHttpsRedirection();
      app.UseCors("AllowFrontend");
      app.UseAuthentication();
      app.UseAuthorization();
      app.MapControllers();
  }
  ```

### 3. Maintain Program.cs Cleanliness
Ensure `Program.cs` is kept simple. It should only call the extensions:
```csharp
using Blog.Presentation.Configurations;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddPresentationServices(builder.Configuration);

var app = builder.Build();
app.Configure();
app.Run();

public partial class Program { }
```
- Never write ad-hoc registrations in `Program.cs`. Always move them to the respective configuration files.
