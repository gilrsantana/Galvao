using Galvao.Application.Common.CQRS;
using Galvao.Application.Common.Interfaces;
using Galvao.Application.UseCases.Roles.Queries;
using Galvao.Shared;

namespace Galvao.Application.UseCases.Roles.QueryHandlers;

public class GetAvailableRolesQueryHandler : IQueryHandler<GetAvailableRolesQuery, List<RoleResponse>>
{
    private readonly IRoleService _roleService;

    public GetAvailableRolesQueryHandler(IRoleService roleService)
    {
        _roleService = roleService;
    }

    public async Task<Result<List<RoleResponse>>> HandleAsync(GetAvailableRolesQuery query, CancellationToken cancellationToken = default)
    {
        return await _roleService.GetAvailableRolesAsync(cancellationToken);
    }
}
