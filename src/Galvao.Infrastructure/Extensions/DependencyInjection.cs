using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Galvao.Application.Common.Interfaces;
using Galvao.Infrastructure.Configurations;
using Galvao.Infrastructure.Identity;
using Galvao.Infrastructure.Persistence;
using Galvao.Infrastructure.Persistence.Repositories;
using Microsoft.AspNetCore.Identity;

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
            options.UseSqlite(connectionString, sqliteOptions =>
            {
                sqliteOptions.CommandTimeout(dbOptions.CommandTimeout);
                if (dbOptions.MaxBatchSize.HasValue)
                {
                    sqliteOptions.MaxBatchSize(dbOptions.MaxBatchSize.Value);
                }
            });

            if (dbOptions.EnableDetailedErrors)
                options.EnableDetailedErrors();
            if (dbOptions.EnableSensitiveDataLogging)
                options.EnableSensitiveDataLogging();
        });

        // Repositories
        services.AddScoped<IMemberRepository, MemberRepository>();
        services.AddScoped<IShowroomItemRepository, ShowroomItemRepository>();
        services.AddScoped<IArticleRepository, ArticleRepository>();
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<GalvaoDbContext>());

        // Identity Services
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IRoleService, RoleService>();

        return services;
    }
}
