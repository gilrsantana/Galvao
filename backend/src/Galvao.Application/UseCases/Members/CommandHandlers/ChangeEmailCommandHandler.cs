using Galvao.Application.Common.CQRS;
using Galvao.Application.Common.Interfaces;
using Galvao.Application.UseCases.Members.Commands;
using Galvao.Domain.MemberContactAggregate.Entities;
using Galvao.Domain.MemberContactAggregate.Enums;
using Galvao.Domain.MemberUserAggregate.Entities;
using Galvao.Shared;

namespace Galvao.Application.UseCases.Members.CommandHandlers;

public class ChangeEmailCommandHandler : ICommandHandler<ChangeEmailCommand>
{
    private readonly IMemberRepository _memberRepository;
    private readonly IMemberContactRepository _memberContactRepository;
    private readonly IEmailContactService _emailContactService;
    private readonly IIdentityService _identityService;
    private readonly IUnitOfWork _unitOfWork;

    public ChangeEmailCommandHandler(
        IMemberRepository memberRepository,
        IMemberContactRepository memberContactRepository,
        IEmailContactService emailContactService,
        IIdentityService identityService,
        IUnitOfWork unitOfWork)
    {
        _memberRepository = memberRepository;
        _memberContactRepository = memberContactRepository;
        _emailContactService = emailContactService;
        _identityService = identityService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> HandleAsync(ChangeEmailCommand command, CancellationToken cancellationToken = default)
    {
        var member = await _memberRepository.GetByIdAsync(command.MemberId, cancellationToken);
        if (member is null)
        {
            return Result.Failure(new Error("Member.NotFound", $"Member with ID '{command.MemberId}' was not found."));
        }

        if (member.Email.Equals(command.NewEmail, StringComparison.OrdinalIgnoreCase))
        {
            return Result.Success();
        }

        var identityResult = await _identityService.ChangeEmailAsync(member.Id, command.NewEmail, cancellationToken);
        if (identityResult.IsFailure)
        {
            return Result.Failure(identityResult.Error);
        }

        var updateMemberResult = UpdateMemberEmail(member, command.NewEmail);
        if (updateMemberResult.IsFailure)
        {
            return updateMemberResult;
        }

        var syncResult = await SyncContactEmailAsync(member, command.NewEmail, cancellationToken);
        if (syncResult.IsFailure)
        {
            return syncResult;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private Result UpdateMemberEmail(Member member, string newEmail)
    {
        var memberResult = member.UpdateEmail(newEmail);
        if (memberResult.IsFailure)
        {
            return Result.Failure(memberResult.Error);
        }

        _memberRepository.Update(member);
        return Result.Success();
    }

    private async Task<Result> SyncContactEmailAsync(Member member, string newEmail, CancellationToken cancellationToken)
    {
        var memberContact = await _memberContactRepository.GetByIdAsync(member.Id, cancellationToken);

        if (memberContact is not null)
        {
            return await UpdateContactEmailAsync(member, memberContact, newEmail, cancellationToken);
        }

        return await CreateContactIfPreferencesAcceptedAsync(member, newEmail, cancellationToken);
    }

    private async Task<Result> UpdateContactEmailAsync(
        Member member,
        MemberContact memberContact,
        string newEmail,
        CancellationToken cancellationToken)
    {
        // Delete old contact in Resend
        var deleteResult = await _emailContactService.DeleteContactAsync(memberContact.ExternalContactId, cancellationToken);
        if (deleteResult.IsFailure)
        {
            return Result.Failure(deleteResult.Error);
        }

        if (member.AcceptNews || member.AcceptPromo)
        {
            return await RecreateAndLinkContactAsync(member, memberContact, newEmail, cancellationToken);
        }

        return UpdateLocalContactAsDeleted(memberContact, newEmail);
    }

    private async Task<Result> RecreateAndLinkContactAsync(
        Member member,
        MemberContact memberContact,
        string newEmail,
        CancellationToken cancellationToken)
    {
        var resendResult = await _emailContactService.CreateContactAsync(
            newEmail,
            member.FirstName,
            member.LastName,
            member.AcceptNews,
            member.AcceptPromo,
            cancellationToken);

        if (resendResult.IsFailure)
        {
            return Result.Failure(resendResult.Error);
        }

        var updateDetailsResult = memberContact.UpdateContactDetails(resendResult.Value, newEmail, null);
        if (updateDetailsResult.IsFailure)
        {
            return updateDetailsResult;
        }

        UpdateSegments(memberContact, member.AcceptNews, member.AcceptPromo);
        _memberContactRepository.Update(memberContact);

        return Result.Success();
    }

    private Result UpdateLocalContactAsDeleted(MemberContact memberContact, string newEmail)
    {
        var updateDetailsResult = memberContact.UpdateContactDetails("DELETED", newEmail, null);
        if (updateDetailsResult.IsFailure)
        {
            return updateDetailsResult;
        }

        UnsubscribeAllSegments(memberContact);
        _memberContactRepository.Update(memberContact);

        return Result.Success();
    }

    private async Task<Result> CreateContactIfPreferencesAcceptedAsync(
        Member member,
        string newEmail,
        CancellationToken cancellationToken)
    {
        if (member.AcceptNews || member.AcceptPromo)
        {
            var resendResult = await _emailContactService.CreateContactAsync(
                newEmail,
                member.FirstName,
                member.LastName,
                member.AcceptNews,
                member.AcceptPromo,
                cancellationToken);

            if (resendResult.IsFailure)
            {
                return Result.Failure(resendResult.Error);
            }

            var newMemberContactResult = MemberContact.Create(
                member.Id,
                resendResult.Value,
                newEmail,
                phoneNumber: null);

            if (newMemberContactResult.IsFailure)
            {
                return Result.Failure(newMemberContactResult.Error);
            }

            var memberContact = newMemberContactResult.Value;
            UpdateSegments(memberContact, member.AcceptNews, member.AcceptPromo);

            await _memberContactRepository.AddAsync(memberContact, cancellationToken);
        }

        return Result.Success();
    }

    private void UpdateSegments(MemberContact memberContact, bool acceptNews, bool acceptPromo)
    {
        if (acceptNews)
        {
            var segment = memberContact.AddEmailSegment(ESegmentType.News);
            if (segment.IsSuccess)
            {
                memberContact.SetEmailSegmentSubscriptionDate(ESegmentType.News);
            }
        }
        else
        {
            var segment = memberContact.GetEmailSegment(ESegmentType.News);
            if (segment.IsSuccess)
            {
                memberContact.SetEmailSegmentUnsubscriptionDate(ESegmentType.News);
            }
        }

        if (acceptPromo)
        {
            var segment = memberContact.AddEmailSegment(ESegmentType.Promo);
            if (segment.IsSuccess)
            {
                memberContact.SetEmailSegmentSubscriptionDate(ESegmentType.Promo);
            }
        }
        else
        {
            var segment = memberContact.GetEmailSegment(ESegmentType.Promo);
            if (segment.IsSuccess)
            {
                memberContact.SetEmailSegmentUnsubscriptionDate(ESegmentType.Promo);
            }
        }
    }

    private void UnsubscribeAllSegments(MemberContact memberContact)
    {
        var newsSegment = memberContact.GetEmailSegment(ESegmentType.News);
        if (newsSegment.IsSuccess)
        {
            memberContact.SetEmailSegmentUnsubscriptionDate(ESegmentType.News);
        }

        var promoSegment = memberContact.GetEmailSegment(ESegmentType.Promo);
        if (promoSegment.IsSuccess)
        {
            memberContact.SetEmailSegmentUnsubscriptionDate(ESegmentType.Promo);
        }
    }
}
