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

namespace Galvao.Presentation.Configurations;

public static class DependencyInjection
{
    public static IServiceCollection AddPresentationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers();

        // CORS
        services.AddCors(options =>
        {
            options.AddPolicy("AllowFrontend", policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });
        });

        // OpenAPI (Native .NET 10)
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

        // Identity Core Services
        services.AddIdentityCore<Account>()
            .AddRoles<Role>()
            .AddEntityFrameworkStores<GalvaoDbContext>()
            .AddDefaultTokenProviders();

        // JWT Settings & Authentication
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
        });

        services.AddAuthorization();

        // Chain Layer Registrations
        services.AddApplication()
                .AddInfrastructure(configuration);

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

        app.UseHttpsRedirection();
        app.UseCors("AllowFrontend");

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();
    }
}
