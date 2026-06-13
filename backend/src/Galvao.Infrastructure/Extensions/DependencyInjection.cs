using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Galvao.Application.Common.Interfaces;
using Galvao.Infrastructure.Configurations;
using Galvao.Infrastructure.Identity;
using Galvao.Infrastructure.Persistence;
using Galvao.Infrastructure.Persistence.Repositories;
using Microsoft.AspNetCore.Identity;
using Galvao.Infrastructure.Services.Emails.Resend;

namespace Galvao.Infrastructure.Extensions;

public static class DependencyInjection
{
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
                if (dbOptions.MaxBatchSize.HasValue)
                {
                    mysqlOptions.MaxBatchSize(dbOptions.MaxBatchSize.Value);
                }
            });

            if (dbOptions.EnableDetailedErrors)
                options.EnableDetailedErrors();
            if (dbOptions.EnableSensitiveDataLogging)
                options.EnableSensitiveDataLogging();
        });

        // Repositories
        services.AddScoped<IMemberRepository, MemberRepository>();
        services.AddScoped<IMemberContactRepository, MemberContactRepository>();
        services.AddScoped<IShowroomItemRepository, ShowroomItemRepository>();
        services.AddScoped<IArticleRepository, ArticleRepository>();
        services.AddScoped<IRemovedUserRepository, RemovedUserRepository>();
        services.AddScoped<IConsentLogRepository, ConsentLogRepository>();
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<GalvaoDbContext>());

        // Identity Services
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IRoleService, RoleService>();

        // Resend Email Integration
        services.Configure<ResendSettings>(configuration.GetSection(ResendSettings.SectionName));
        services.AddHttpClient<IEmailContactService, ResendEmailContactService>((sp, client) =>
        {
            var settings = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ResendSettings>>().Value;
            client.BaseAddress = new Uri("https://api.resend.com/");
            client.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", settings.ManagerApiKey);
        });

        return services;
    }
}
