using System.Text;
using Galvao.Application.Extensions;
using Galvao.Infrastructure.Extensions;
using Galvao.Infrastructure.Identity;
using Galvao.Infrastructure.Persistence;
using Galvao.Presentation.Middlewares;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using Hangfire;
using Hangfire.MySql;
using Microsoft.Extensions.Options;

namespace Galvao.Presentation.Configurations;

public static class DependencyInjection
{
    public static IServiceCollection AddPresentationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers();

        services.ConfigureCors(configuration)
            .ConfigureOpenApi()
            // Identity Core Services
            .AddIdentityCore<Account>(options =>
            {
                options.SignIn.RequireConfirmedEmail = true;
            })
            .AddRoles<Role>()
            .AddEntityFrameworkStores<GalvaoDbContext>()
            .AddDefaultTokenProviders();

        services.Configure<DataProtectionTokenProviderOptions>(options =>
        {
            options.TokenLifespan = TimeSpan.FromHours(24);
        });
        
        // JWT Settings & Authentication
        SetJwtConfiguration(services, configuration)
            .AddAuthorization();

        // Hangfire background jobs configuration
        services.AddHangfireConfiguration(configuration)
            // Chain Layer Registrations
            .AddApplication()
            .AddInfrastructure(configuration);
        
        return services;
    }

    private static IServiceCollection ConfigureOpenApi(this IServiceCollection services)
    {
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
                document.Components.SecuritySchemes.TryAdd("Bearer", scheme);

                var schemeReference = new OpenApiSecuritySchemeReference("Bearer", document);
                var requirement = new OpenApiSecurityRequirement
                {
                    { schemeReference, new List<string>() }
                };

                document.Security ??= new List<OpenApiSecurityRequirement>();
                document.Security.Add(requirement);

                return Task.CompletedTask;
            });
        });

        return services;
    }

    private static IServiceCollection ConfigureCors(this IServiceCollection services, IConfiguration configuration)
    {
        var allowedOrigins = configuration
            .GetSection("CorsSettings:AllowedOrigins")
            .Get<string[]>() ?? Array.Empty<string>();
        services.AddCors(options =>
        {
            options.AddPolicy("AllowFrontend", policy =>
            {
                policy.WithOrigins(allowedOrigins)
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            });
        });
        return  services;
    }

    private static IServiceCollection AddHangfireConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection") 
                               ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");

        // MySqlConnector (used by Hangfire) expects SslMode=None instead of SslMode=Disabled, and requires Allow User Variables=true
        var hangfireConnectionString = connectionString.Replace("SslMode=Disabled", "SslMode=None", StringComparison.OrdinalIgnoreCase);
        if (!hangfireConnectionString.Contains("Allow User Variables", StringComparison.OrdinalIgnoreCase) && 
            !hangfireConnectionString.Contains("AllowUserVariables", StringComparison.OrdinalIgnoreCase))
        {
            hangfireConnectionString += ";Allow User Variables=true";
        }

        services.AddHangfire(config =>
        {
            config.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseStorage(new MySqlStorage(
                    hangfireConnectionString,
                    new MySqlStorageOptions
                    {
                        QueuePollInterval = TimeSpan.FromSeconds(30),
                        JobExpirationCheckInterval = TimeSpan.FromHours(1),
                        CountersAggregateInterval = TimeSpan.FromMinutes(5),
                        PrepareSchemaIfNecessary = true,
                        DashboardJobListLimit = 50000,
                        TransactionTimeout = TimeSpan.FromMinutes(1),
                        TablesPrefix = "hangfire_"
                    }));
        });

        services.AddHangfireServer(options =>
        {
            // Shared hosting has extremely low max_user_connections limits (often 10-20).
            // Reducing the worker count to 2 prevents Hangfire from exhausting the connection pool.
            options.WorkerCount = 2;
            options.ServerName = "hangfire_server_1";
        });

        return services;
    }

    private static IServiceCollection SetJwtConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("JwtSettings").Get<JwtSettings>() 
                          ?? throw new InvalidOperationException("JwtSettings section is missing from configuration.");
        
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
                    ClockSkew = TimeSpan.Zero
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["token"];
                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) && 
                            (path.StartsWithSegments("/api/admin/hangfire-redirect") || path.StartsWithSegments("/hangfire")))
                        {
                            context.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    }
                };
            });
        return services;
    }

    public static void Configure(this WebApplication app)
    {
        // Exception handling middleware must be FIRST in pipeline
        app.UseMiddleware<CustomExceptionHandlingMiddleware>();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.MapScalarApiReference(options =>
            {
                options.WithTitle("Galvao Web API")
                       .WithTheme(ScalarTheme.Moon)
                       .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
            });
        }

        if (!string.Equals(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"), "true", StringComparison.OrdinalIgnoreCase))
        {
            app.UseHttpsRedirection();
        }
        app.UseCors("AllowFrontend");

        app.UseAuthentication();
        app.UseAuthorization();

        // Mount Hangfire dashboard with custom authorization
        var jwtSettingsOptions = app.Services.GetRequiredService<IOptions<JwtSettings>>().Value;
        app.MapHangfireDashboard("/hangfire", new DashboardOptions
        {
            Authorization = new[] { new HangfireAdminAuthorizationFilter(jwtSettingsOptions) },
            IgnoreAntiforgeryToken = true
        });

        app.MapControllers();
    }
}
