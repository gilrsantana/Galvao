using Galvao.Application.Common.CQRS;

namespace Galvao.Application.UseCases.Members.Commands;

public record PurgeUserCommand(Guid MemberId, string Password) : ICommand;
