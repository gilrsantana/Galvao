using Galvao.Application.Common.CQRS;

namespace Galvao.Application.UseCases.Roles.Commands;

public record CreateRoleCommand(string RoleName, string Description) : ICommand;
