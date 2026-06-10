using Galvao.Application.Common.CQRS;

namespace Galvao.Application.UseCases.Members.Commands;

public record UpdateMemberProfileCommand(Guid MemberId, string DisplayName, string FirstName, string LastName) : ICommand;
