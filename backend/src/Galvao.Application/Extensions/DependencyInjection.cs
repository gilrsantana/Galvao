using Galvao.Application.ApplicationJobs.Interfaces;
using Galvao.Application.ApplicationJobs.Jobs;
using Galvao.Application.Common.CQRS;
using Galvao.Application.Common.Interfaces;
using Galvao.Application.UseCases.Articles.CommandHandlers;
using Galvao.Application.UseCases.Articles.Commands;
using Galvao.Application.UseCases.Articles.Queries;
using Galvao.Application.UseCases.Articles.QueryHandlers;
using Galvao.Application.UseCases.Members.CommandHandlers;
using Galvao.Application.UseCases.Members.Commands;
using Galvao.Application.UseCases.Members.Queries;
using Galvao.Application.UseCases.Members.QueryHandlers;
using Galvao.Application.UseCases.Roles.CommandHandlers;
using Galvao.Application.UseCases.Roles.Commands;
using Galvao.Application.UseCases.Roles.Queries;
using Galvao.Application.UseCases.Roles.QueryHandlers;
using Galvao.Application.UseCases.Showroom.CommandHandlers;
using Galvao.Application.UseCases.Showroom.Commands;
using Galvao.Application.UseCases.Showroom.Queries;
using Galvao.Application.UseCases.Showroom.QueryHandlers;
using Galvao.Shared;
using Microsoft.Extensions.DependencyInjection;

namespace Galvao.Application.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Jobs
        services.AddScoped<ICrmSyncJob, CrmSyncJob>();

        // Commands
        services.AddScoped<ICommandHandler<RegisterMemberCommand, Guid>, RegisterMemberCommandHandler>();
        services.AddScoped<ICommandHandler<UpdateMemberProfileCommand>, UpdateMemberProfileCommandHandler>();
        services.AddScoped<ICommandHandler<ChangeEmailCommand>, ChangeEmailCommandHandler>();
        services.AddScoped<ICommandHandler<ChangePasswordCommand>, ChangePasswordCommandHandler>();
        services.AddScoped<ICommandHandler<UpdateMarketingPreferencesCommand>, UpdateMarketingPreferencesCommandHandler>();
        services.AddScoped<ICommandHandler<PurgeUserCommand>, PurgeUserCommandHandler>();
        services.AddScoped<ICommandHandler<CreateShowroomItemCommand, Guid>, CreateShowroomItemCommandHandler>();
        services.AddScoped<ICommandHandler<UpdateShowroomItemCommand>, UpdateShowroomItemCommandHandler>();
        services.AddScoped<ICommandHandler<AddShowroomItemPhotoCommand, Guid>, AddShowroomItemPhotoCommandHandler>();
        services.AddScoped<ICommandHandler<RemoveShowroomItemPhotoCommand>, RemoveShowroomItemPhotoCommandHandler>();
        services.AddScoped<ICommandHandler<UpdateShowroomItemPhotoCommand>, UpdateShowroomItemPhotoCommandHandler>();
        services.AddScoped<ICommandHandler<CreateArticleCommand, Guid>, CreateArticleCommandHandler>();
        services.AddScoped<ICommandHandler<UpdateArticleCommand>, UpdateArticleCommandHandler>();
        services.AddScoped<ICommandHandler<PublishArticleCommand>, PublishArticleCommandHandler>();
        services.AddScoped<ICommandHandler<CreateRoleCommand>, CreateRoleCommandHandler>();
        services.AddScoped<ICommandHandler<AssignRoleCommand>, AssignRoleCommandHandler>();
        services.AddScoped<ICommandHandler<RemoveRoleCommand>, RemoveRoleCommandHandler>();

        // Queries
        services.AddScoped<IQueryHandler<GetMemberByIdQuery, MemberResponse>, GetMemberByIdQueryHandler>();
        services.AddScoped<IQueryHandler<GetShowroomItemByIdQuery, ShowroomItemResponse>, GetShowroomItemByIdQueryHandler>();
        services.AddScoped<IQueryHandler<GetPagedShowroomItemsQuery, PagedResponse<ShowroomItemResponse>>, GetPagedShowroomItemsQueryHandler>();
        services.AddScoped<IQueryHandler<GetArticleByIdQuery, ArticleResponse>, GetArticleByIdQueryHandler>();
        services.AddScoped<IQueryHandler<GetArticleBySlugQuery, ArticleResponse>, GetArticleBySlugQueryHandler>();
        services.AddScoped<IQueryHandler<GetPagedArticlesQuery, PagedResponse<ArticleResponse>>, GetPagedArticlesQueryHandler>();
        services.AddScoped<IQueryHandler<GetUserRolesQuery, List<string>>, GetUserRolesQueryHandler>();
        services.AddScoped<IQueryHandler<GetAvailableRolesQuery, List<RoleResponse>>, GetAvailableRolesQueryHandler>();

        return services;
    }
}
