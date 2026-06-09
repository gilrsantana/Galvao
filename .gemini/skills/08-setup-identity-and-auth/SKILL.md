---
name: setup-identity-and-auth
description: Set up ASP.NET Core Identity, JWT authentication, token validation, and refresh token rotation in a new .NET Web API project.
---

# Skill: Setting Up Identity and JWT Authentication

This skill guides you through setting up ASP.NET Core Identity, JWT Bearer authentication, and refresh token rotation.

---

## Steps

### 1. Install Required NuGet Packages
Ensure the following packages are installed:
- **Blog.Infrastructure**:
  - `Microsoft.AspNetCore.Identity.EntityFrameworkCore`
  - `System.IdentityModel.Tokens.Jwt`
- **Blog.Presentation**:
  - `Microsoft.AspNetCore.Authentication.JwtBearer`

---

### 2. Define the Custom Identity Entities
In your Infrastructure project (under `Identity/`), create the Identity entities:

- **`Account.cs`** (inheriting from `IdentityUser<Guid>`):
  ```csharp
  using Microsoft.AspNetCore.Identity;

  namespace Blog.Infrastructure.Identity;

  public class Account : IdentityUser<Guid>
  {
      public string RefreshToken { get; private set; } = string.Empty;
      public DateTime RefreshTokenExpiryTime { get; private set; }

      public Account() { }

      private Account(Guid id, string email)
      {
          Id = id;
          Email = email;
          UserName = email;
      }

      public static Account Create(Guid id, string email) => new Account(id, email);

      public void UpdateRefreshToken(string refreshToken, DateTime expiryTime)
      {
          RefreshToken = refreshToken;
          RefreshTokenExpiryTime = expiryTime;
      }

      public void UpdateLockoutStatus(bool blocked)
      {
          LockoutEnd = blocked ? DateTimeOffset.UtcNow.AddYears(100) : null;
      }
  }
  ```

- **`Role.cs`** (inheriting from `IdentityRole<Guid>`):
  ```csharp
  using Microsoft.AspNetCore.Identity;

  namespace Blog.Infrastructure.Identity;

  public class Role : IdentityRole<Guid>
  {
      public string Description { get; private set; } = string.Empty;

      private Role() { }

      private Role(string name, string description)
      {
          Name = name;
          NormalizedName = name.ToUpperInvariant();
          Description = description;
      }

      public static Role Create(string name, string description) => new Role(name, description);
  }
  ```

---

### 3. Rename Identity Database Tables
By default, EF Core creates tables named `AspNetUsers`, `AspNetRoles`, etc. Override this behavior by applying Entity Type Configurations in `Persistence/Configurations/`:

- **AccountConfiguration**:
  ```csharp
  builder.ToTable("Accounts");
  ```
- **RoleConfiguration**:
  ```csharp
  builder.ToTable("Roles");
  ```
- **AccountRoleConfiguration**:
  ```csharp
  builder.ToTable("AccountRoles");
  ```
Apply the configurations using `builder.ApplyConfigurationsFromAssembly` inside the `BlogDbContext` class.

---

### 4. Create JWT Token Configuration
Define a `JwtSettings.cs` options class inside the Infrastructure layer:
```csharp
namespace Blog.Infrastructure.Identity;

public class JwtSettings
{
    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpiryInMinutes { get; set; }
}
```
Add settings to `appsettings.json`:
```json
"JwtSettings": {
  "Secret": "A_SUPER_LONG_JWT_SIGNING_KEY_EXCEEDING_256_BITS",
  "Issuer": "BlogAPI",
  "Audience": "BlogAPI",
  "ExpiryInMinutes": 60
}
```

---

### 5. Wire Identity and Authentication in Dependency Injection
Open the Presentation layer's Composition Root config (`Configurations/DependencyInjection.cs`):

1. **Add Identity Services**:
   ```csharp
   services.AddIdentityCore<Account>()
       .AddRoles<Role>()
       .AddEntityFrameworkStores<BlogDbContext>();
   ```

2. **Configure Authentication & JWT Bearer Options**:
   ```csharp
   var jwtSettings = configuration.GetSection("JwtSettings").Get<JwtSettings>();
   services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));

   services.AddAuthentication(options =>
   {
       options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
       options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
   })
   .AddJwtBearer(options =>
   {
       options.TokenValidationParameters = new TokenValidationParameters
       {
           ValidateIssuer = true,
           ValidateAudience = true,
           ValidateLifetime = true,
           ValidateIssuerSigningKey = true,
           ValidIssuer = jwtSettings.Issuer,
           ValidAudience = jwtSettings.Audience,
           IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
           ClockSkew = TimeSpan.Zero // Strict immediate expiration validation
       };
   });
   ```

3. **Map Pipeline Middlewares** inside `Configure(WebApplication app)`:
   ```csharp
   app.UseAuthentication();
   app.UseAuthorization();
   ```

---

### 6. Implement the IdentityService
Create an interface `IIdentityService` in the Application layer, and implement it in `Blog.Infrastructure/Identity/IdentityService.cs`. Use `UserManager<Account>` and `RoleManager<Role>` to manage credentials:

- **GenerateAccessToken**: Generate claims (Sub, Jti, Email, roles) and write the token using `JwtSecurityTokenHandler`.
- **GenerateRefreshToken**: Create a cryptographically secure random token:
  ```csharp
  var randomNumber = new byte[64];
  using var rng = RandomNumberGenerator.Create();
  rng.GetBytes(randomNumber);
  var refreshToken = Convert.ToBase64String(randomNumber);
  ```
- **Rotate Tokens**: Upon receiving a validation request, call `GetPrincipalFromExpiredToken`, load the associated `Account`, match the `RefreshToken` and its expiration, and issue a fresh Access Token + rotated Refresh Token.
