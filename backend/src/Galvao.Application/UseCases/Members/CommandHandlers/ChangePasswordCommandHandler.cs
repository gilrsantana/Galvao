using Galvao.Application.Common.CQRS;
using Galvao.Application.Common.Interfaces;
using Galvao.Application.UseCases.Members.Commands;
using Galvao.Shared;

namespace Galvao.Application.UseCases.Members.CommandHandlers;

public class ChangePasswordCommandHandler : ICommandHandler<ChangePasswordCommand>
{
    private readonly IIdentityService _identityService;

    public ChangePasswordCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<Result> HandleAsync(ChangePasswordCommand command, CancellationToken cancellationToken = default)
    {
        return await _identityService.ChangePasswordAsync(
            command.MemberId,
            command.CurrentPassword,
            command.NewPassword,
            cancellationToken);
    }
}
