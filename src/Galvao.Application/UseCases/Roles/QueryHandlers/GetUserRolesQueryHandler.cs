using Galvao.Application.Common.CQRS;
using Galvao.Application.Common.Interfaces;
using Galvao.Application.UseCases.Roles.Queries;
using Galvao.Shared;

namespace Galvao.Application.UseCases.Roles.QueryHandlers;

public class GetUserRolesQueryHandler : IQueryHandler<GetUserRolesQuery, List<string>>
{
    private readonly IRoleService _roleService;

    public GetUserRolesQueryHandler(IRoleService roleService)
    {
        _roleService = roleService;
    }

    public async Task<Result<List<string>>> HandleAsync(GetUserRolesQuery query, CancellationToken cancellationToken = default)
    {
        return await _roleService.GetUserRolesAsync(query.UserId, cancellationToken);
    }
}
