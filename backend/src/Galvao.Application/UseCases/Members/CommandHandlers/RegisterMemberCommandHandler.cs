using Galvao.Application.Common.CQRS;
using Galvao.Application.Common.Interfaces;
using Galvao.Application.Jobs.Interfaces;
using Galvao.Application.UseCases.Members.Commands;
using Galvao.Domain.MemberContactAggregate.Entities;
using Galvao.Domain.MemberUserAggregate.Entities;
using Galvao.Shared;

namespace Galvao.Application.UseCases.Members.CommandHandlers;

public class RegisterMemberCommandHandler(
    IIdentityService identityService,
    IUnitOfWork unitOfWork,
    IMemberRepository memberRepository,
    IBackgroundJobService backgroundJobService) : ICommandHandler<RegisterMemberCommand, Guid>
{
    private readonly IIdentityService _identityService = identityService;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IMemberRepository _memberRepository = memberRepository;
    private readonly IBackgroundJobService _backgroundJobService = backgroundJobService;

    public async Task<Result<Guid>> HandleAsync(RegisterMemberCommand command, CancellationToken cancellationToken = default)
    {
        var emailUniqueResult = await _identityService.CheckEmailUniquenessAsync(command.Email);
        if (emailUniqueResult.IsFailure)
        {
            return Result.Failure<Guid>(emailUniqueResult.Error);
        }

        var memberResult = Member.Create(
            command.Email,
            command.DisplayName,
            command.FirstName,
            command.LastName,
            command.AcceptNews,
            command.AcceptPromo
        );
        if (memberResult.IsFailure)
        {
            return Result.Failure<Guid>(memberResult.Error);
        }

        var member = memberResult.Value;

        var result = await _identityService.RegisterAsync(
            member.Id,
            command.Email,
            command.Password,
            cancellationToken);

        if (result.IsFailure)
        {
            return Result.Failure<Guid>(result.Error);
        }

        await _memberRepository.AddAsync(member, cancellationToken);

        if (command.AcceptNews || command.AcceptPromo)
        {
            _backgroundJobService.Enqueue<ICrmSyncJob>(job =>
                job.SyncContactAsync(member.Id, member.Email, member.FirstName, member.LastName, member.AcceptNews, member.AcceptPromo, CancellationToken.None));
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return member.Id;
    }
}
