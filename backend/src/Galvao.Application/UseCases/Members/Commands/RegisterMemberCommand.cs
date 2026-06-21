using Galvao.Application.Common.CQRS;

namespace Galvao.Application.UseCases.Members.Commands;

public record RegisterMemberCommand(
    string Email,
    string Password,
    string DisplayName,
    string FirstName,
    string LastName,
    bool AcceptNews,
    bool AcceptPromo
) : ICommand<Guid>;