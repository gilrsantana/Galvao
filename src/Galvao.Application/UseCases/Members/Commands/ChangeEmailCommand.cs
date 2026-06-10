using Galvao.Application.Common.CQRS;

namespace Galvao.Application.UseCases.Members.Commands;

public record ChangeEmailCommand(Guid MemberId, string NewEmail) : ICommand;
