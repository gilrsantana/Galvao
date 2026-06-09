using Galvao.Application.Common.CQRS;

namespace Galvao.Application.UseCases.Roles.Commands;

public record AssignRoleCommand(Guid UserId, string RoleName) : ICommand;
