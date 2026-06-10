using Galvao.Application.Common.CQRS;
using Galvao.Application.Common.Interfaces;
using Galvao.Application.UseCases.Roles.Commands;
using Galvao.Shared;

namespace Galvao.Application.UseCases.Roles.CommandHandlers;

public class RemoveRoleCommandHandler : ICommandHandler<RemoveRoleCommand>
{
    private readonly IRoleService _roleService;

    public RemoveRoleCommandHandler(IRoleService roleService)
    {
        _roleService = roleService;
    }

    public async Task<Result> HandleAsync(RemoveRoleCommand command, CancellationToken cancellationToken = default)
    {
        return await _roleService.RemoveRoleAsync(command.UserId, command.RoleName, cancellationToken);
    }
}
