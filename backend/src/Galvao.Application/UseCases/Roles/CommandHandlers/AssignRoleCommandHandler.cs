using Galvao.Application.Common.CQRS;
using Galvao.Application.Common.Interfaces;
using Galvao.Application.UseCases.Roles.Commands;
using Galvao.Shared;

namespace Galvao.Application.UseCases.Roles.CommandHandlers;

public class AssignRoleCommandHandler : ICommandHandler<AssignRoleCommand>
{
    private readonly IRoleService _roleService;

    public AssignRoleCommandHandler(IRoleService roleService)
    {
        _roleService = roleService;
    }

    public async Task<Result> HandleAsync(AssignRoleCommand command, CancellationToken cancellationToken = default)
    {
        return await _roleService.AssignRoleAsync(command.UserId, command.RoleName, cancellationToken);
    }
}
