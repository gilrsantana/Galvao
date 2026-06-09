# Rule: Dependency Injection and Modular Service Registration

## Metadata
- **ID**: RULE-009-DEPENDENCY-INJECTION
- **Scope**: Entire Solution
- **Target Layers**: Application, Infrastructure, Presentation, Program.cs
- **Status**: Active

## Overview
Dependency Injection (DI) registration must be modular and decentralized. Each layer is responsible for registering its own dependencies. The Presentation layer acts as the orchestrator (Composition Root) through dedicated extension configurations, ensuring the `Program.cs` file remains clean and contains only configuration dispatching.

---

## 1. Modular Layer Registration (`Extensions/`)
Every project layer that requires dependency registration must define an `Extensions/` directory containing a static `DependencyInjection.cs` class.

### A. Application Layer (`Blog.Application/Extensions/DependencyInjection.cs`)
- Register Use Case handlers (Commands and Queries) individually as `Scoped`.
- Use comment blocks to separate `// Commands` and `// Queries`.
- **Namespace**: `Blog.Application.Extensions`
- **Method Signature**: `public static IServiceCollection AddApplication(this IServiceCollection services)`

### B. Infrastructure Layer (`Blog.Infrastructure/Extensions/DependencyInjection.cs`)
- Register repositories, data context, database configurations, options binding, and infrastructure services.
- **Namespace**: `Blog.Infrastructure.Extensions`
- **Method Signature**: `public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)`

---

## 2. Presentation Layer Orchestration (`Configurations/`)
- The `Blog.Presentation` layer contains a `Configurations/` folder.
- The `Configurations/DependencyInjection.cs` file under the namespace `Blog.Presentation.Configurations` houses the composition root.
- It must implement two extension methods:
  1. **Service Registration**:
     `public static IServiceCollection AddPresentationServices(this IServiceCollection services, IConfiguration configuration)`
     - Registers controllers, CORS policies, OpenAPI configuration, Scalar endpoints, and JWT Bearer security.
     - Directs references and chains modular registrations from other layers:
       ```csharp
       services.AddApplication()
               .AddInfrastructure(configuration);
       ```
  2. **Pipeline Configuration**:
     `public static void Configure(this WebApplication app)`
     - Builds the middleware pipeline (Exceptions middleware, HTTP redirection, CORS, Authentication, Authorization, MapControllers).

---

## 3. The Clean `Program.cs` Rule
- The `Program.cs` file must only call the configured extension methods.
- No direct database configurations, authentication setup, CORS configuration, or individual registrations should be written directly in `Program.cs`.
- Always expose a `public partial class Program` at the end of `Program.cs` to allow integration test hosting.

### Standard `Program.cs` Template:
```csharp
using Blog.Presentation.Configurations;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddPresentationServices(builder.Configuration);

var app = builder.Build();
app.Configure();
app.Run();

public partial class Program { }
```
