using Galvao.Application.Common.CQRS;

namespace Galvao.Application.UseCases.Members.Commands;

public record ChangePasswordCommand(Guid MemberId, string CurrentPassword, string NewPassword) : ICommand;
