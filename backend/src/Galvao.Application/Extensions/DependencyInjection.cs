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
        services
            .AddJobs()
            .AddCommands()
            .AddQueries();

        return services;
    }

    private static IServiceCollection AddQueries(this IServiceCollection services) =>
        services
            .AddScoped<IQueryHandler<GetMemberByIdQuery, MemberResponse>, GetMemberByIdQueryHandler>()
            .AddScoped<IQueryHandler<GetShowroomItemByIdQuery, ShowroomItemResponse>, GetShowroomItemByIdQueryHandler>()
            .AddScoped<IQueryHandler<GetPagedShowroomItemsQuery, PagedResponse<ShowroomItemResponse>>, GetPagedShowroomItemsQueryHandler>()
            .AddScoped<IQueryHandler<GetArticleByIdQuery, ArticleResponse>, GetArticleByIdQueryHandler>()
            .AddScoped<IQueryHandler<GetArticleBySlugQuery, ArticleResponse>, GetArticleBySlugQueryHandler>()
            .AddScoped<IQueryHandler<GetPagedArticlesQuery, PagedResponse<ArticleResponse>>, GetPagedArticlesQueryHandler>()
            .AddScoped<IQueryHandler<GetUserRolesQuery, List<string>>, GetUserRolesQueryHandler>()
            .AddScoped<IQueryHandler<GetAvailableRolesQuery, List<RoleResponse>>, GetAvailableRolesQueryHandler>();

    private static IServiceCollection AddCommands(this IServiceCollection services) =>
        services
            .AddScoped<ICommandHandler<RegisterMemberCommand, Guid>, RegisterMemberCommandHandler>()
            .AddScoped<ICommandHandler<UpdateMemberProfileCommand>, UpdateMemberProfileCommandHandler>()
            .AddScoped<ICommandHandler<ChangeEmailCommand>, ChangeEmailCommandHandler>()
            .AddScoped<ICommandHandler<ChangePasswordCommand>, ChangePasswordCommandHandler>()
            .AddScoped<ICommandHandler<UpdateMarketingPreferencesCommand>, UpdateMarketingPreferencesCommandHandler>()
            .AddScoped<ICommandHandler<PurgeUserCommand>, PurgeUserCommandHandler>()
            .AddScoped<ICommandHandler<CreateShowroomItemCommand, Guid>, CreateShowroomItemCommandHandler>()
            .AddScoped<ICommandHandler<UpdateShowroomItemCommand>, UpdateShowroomItemCommandHandler>()
            .AddScoped<ICommandHandler<AddShowroomItemPhotoCommand, Guid>, AddShowroomItemPhotoCommandHandler>()
            .AddScoped<ICommandHandler<RemoveShowroomItemPhotoCommand>, RemoveShowroomItemPhotoCommandHandler>()
            .AddScoped<ICommandHandler<UpdateShowroomItemPhotoCommand>, UpdateShowroomItemPhotoCommandHandler>()
            .AddScoped<ICommandHandler<CreateArticleCommand, Guid>, CreateArticleCommandHandler>()
            .AddScoped<ICommandHandler<UpdateArticleCommand>, UpdateArticleCommandHandler>()
            .AddScoped<ICommandHandler<PublishArticleCommand>, PublishArticleCommandHandler>()
            .AddScoped<ICommandHandler<CreateRoleCommand>, CreateRoleCommandHandler>()
            .AddScoped<ICommandHandler<AssignRoleCommand>, AssignRoleCommandHandler>()
            .AddScoped<ICommandHandler<RemoveRoleCommand>, RemoveRoleCommandHandler>();

    private static IServiceCollection AddJobs(this IServiceCollection services) =>
        services
            .AddScoped<ICrmSyncJob, CrmSyncJob>();

}
