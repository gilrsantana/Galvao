using Galvao.Application.Common.CQRS;
using Galvao.Application.Common.Interfaces;
using Galvao.Application.UseCases.Roles.Commands;
using Galvao.Shared;

namespace Galvao.Application.UseCases.Roles.CommandHandlers;

public class CreateRoleCommandHandler : ICommandHandler<CreateRoleCommand>
{
    private readonly IRoleService _roleService;

    public CreateRoleCommandHandler(IRoleService roleService)
    {
        _roleService = roleService;
    }

    public async Task<Result> HandleAsync(CreateRoleCommand command, CancellationToken cancellationToken = default)
    {
        return await _roleService.CreateRoleAsync(command.RoleName, command.Description, cancellationToken);
    }
}
