using Galvao.Application.Common.CQRS;

namespace Galvao.Application.UseCases.Roles.Commands;

public record RemoveRoleCommand(Guid UserId, string RoleName) : ICommand;
