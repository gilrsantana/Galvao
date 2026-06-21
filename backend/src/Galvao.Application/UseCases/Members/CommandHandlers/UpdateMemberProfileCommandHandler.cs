using Galvao.Application.Common.CQRS;
using Galvao.Application.Common.Interfaces;
using Galvao.Application.UseCases.Members.Commands;
using Galvao.Domain.MemberUserAggregate.Entities;
using Galvao.Shared;

namespace Galvao.Application.UseCases.Members.CommandHandlers;

public class UpdateMemberProfileCommandHandler : ICommandHandler<UpdateMemberProfileCommand>
{
    private readonly IMemberRepository _memberRepository;
    private readonly IMemberContactRepository _memberContactRepository;
    private readonly IEmailContactService _emailContactService;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateMemberProfileCommandHandler(
        IMemberRepository memberRepository,
        IMemberContactRepository memberContactRepository,
        IEmailContactService emailContactService,
        IUnitOfWork unitOfWork)
    {
        _memberRepository = memberRepository;
        _memberContactRepository = memberContactRepository;
        _emailContactService = emailContactService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> HandleAsync(UpdateMemberProfileCommand command, CancellationToken cancellationToken = default)
    {
        var member = await _memberRepository.GetByIdAsync(command.MemberId, cancellationToken);
        if (member is null)
        {
            return Result.Failure(new Error("Member.NotFound", $"Member with ID '{command.MemberId}' was not found."));
        }

        var nameChanged = HasNameChanged(member, command);

        var updateProfileResult = UpdateMemberProfile(member, command);
        if (updateProfileResult.IsFailure)
        {
            return updateProfileResult;
        }

        var syncResult = await SyncContactNameIfNeededAsync(member.Id, nameChanged, command, cancellationToken);
        if (syncResult.IsFailure)
        {
            return syncResult;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private bool HasNameChanged(Member member, UpdateMemberProfileCommand command)
    {
        return member.FirstName != command.FirstName || member.LastName != command.LastName;
    }

    private Result UpdateMemberProfile(Member member, UpdateMemberProfileCommand command)
    {
        var result = member.UpdateProfile(command.DisplayName, command.FirstName, command.LastName);
        if (result.IsFailure)
        {
            return result;
        }

        _memberRepository.Update(member);
        return Result.Success();
    }

    private async Task<Result> SyncContactNameIfNeededAsync(
        Guid memberId,
        bool nameChanged,
        UpdateMemberProfileCommand command,
        CancellationToken cancellationToken)
    {
        if (!nameChanged)
        {
            return Result.Success();
        }

        var memberContact = await _memberContactRepository.GetByIdAsync(memberId, cancellationToken);
        if (memberContact is not null && memberContact.ExternalContactId != "DELETED")
        {
            var updateContactResult = await _emailContactService.UpdateContactAsync(
                memberContact.ExternalContactId,
                command.FirstName,
                command.LastName,
                unsubscribed: false,
                cancellationToken);

            if (updateContactResult.IsFailure)
            {
                return Result.Failure(updateContactResult.Error);
            }
        }

        return Result.Success();
    }
}
