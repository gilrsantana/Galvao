# Rule: API Controllers and Presentation Layer Error Handling

## Metadata
- **ID**: RULE-008-API-CONTROLLERS-ERROR-HANDLING
- **Scope**: Blog.Presentation
- **Target Types**: Controllers, Middlewares, OpenAPI, Scalar
- **Status**: Active

## Overview
All HTTP controllers must inherit from a unified base class that maps business logic `Result` outcomes to standardized HTTP response structures using RFC 7807 `ProblemDetails`. Unhandled runtime exceptions must be caught, logged, and formatted uniformly by a dedicated middleware registered at the beginning of the request pipeline. API documentation must be exposed via Native OpenAPI and Scalar.

---

## 1. Controller Base Patterns (`ApiControllerBase`)
- All API controllers must inherit from the abstract class `ApiControllerBase`.
- The base class must be decorated with:
  - `[ApiController]`
  - `[Authorize]` (so authentication is mandatory by default)
  - `[Route("api/[controller]")]`
- **Result Dispatching**: Controllers must delegate response mapping to the protected methods:
  - `HandleResult(Result result)` -> Returns `200 OK` on success, or maps errors.
  - `HandleResult<T>(Result<T> result)` -> Returns `200 OK` with the payload on success, or maps errors.

### Error to HTTP Map:
Inside `MapFailureResult`, error codes must map to HTTP statuses:
- `"Auth.InvalidCredentials"` / `"Auth.InvalidToken"` / `"Token.Expired"` -> `401 Unauthorized`
- Code contains `"NotFound"` -> `404 Not Found`
- Code contains `"Required"` or `"Invalid"` -> `400 Bad Request`
- Default case -> `400 Bad Request`
- All failure outputs must be wrapped in a `ProblemDetails` object.

---

## 2. OpenAPI & Scalar API Reference Integration
The Presentation layer uses native ASP.NET Core OpenAPI services combined with **Scalar** for modern web-based API documentation (replacing Swagger/Swashbuckle).

### Configuration Standards:
- **Service Registration** (`Configurations/DependencyInjection.cs`):
  Expose OpenAPI services and add a document transformer to configure global JWT Authorization headers:
  ```csharp
  services.AddOpenApi(options =>
  {
      options.AddDocumentTransformer((document, context, cancellationToken) =>
      {
          var scheme = new OpenApiSecurityScheme
          {
              Type = SecuritySchemeType.Http,
              Name = "Authorization",
              In = ParameterLocation.Header,
              Scheme = "bearer",
              BearerFormat = "JWT",
              Description = "Input your JWT Bearer token to access protected endpoints."
          };

          document.Components ??= new OpenApiComponents();
          document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
          document.Components.SecuritySchemes.Add("Bearer", scheme);

          var requirement = new OpenApiSecurityRequirement
          {
              { new OpenApiSecuritySchemeReference("Bearer", document), new List<string>() }
          };
          
          document.Security ??= new List<OpenApiSecurityRequirement>();
          document.Security.Add(requirement);

          return Task.CompletedTask;
      });
  });
  ```
- **Middleware Pipeline Mapping**:
  Map OpenAPI endpoints and the Scalar API reference interface under development environment checks:
  ```csharp
  if (app.Environment.IsDevelopment())
  {
      app.MapOpenApi();
      app.MapScalarApiReference(options =>
      {
          options.WithTitle("Blog Web API")
                 .WithTheme(ScalarTheme.Moon)
                 .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
      });
  }
  ```

---

## 3. Unhandled Exception Handling Middleware
- All unhandled exceptions must be caught by `CustomExceptionHandlingMiddleware`.
- **Pipeline Position**: This middleware must be configured as the **very first** element in `Configurations/DependencyInjection.cs`.
- **Response Format**: Content-Type must be `application/problem+json`, status code `500 Internal Server Error`, returning a `ProblemDetails` payload.
- **Logging**: Unhandled exceptions must be logged using `ILogger.LogError(...)`.
