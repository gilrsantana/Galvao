using Galvao.Application.Common.CQRS;
using Galvao.Application.Common.Interfaces;
using Galvao.Application.UseCases.Members.Commands;
using Galvao.Domain.MemberContactAggregate.Entities;
using Galvao.Domain.MemberContactAggregate.Enums;
using Galvao.Domain.MemberUserAggregate.Entities;
using Galvao.Shared;

namespace Galvao.Application.UseCases.Members.CommandHandlers;

public class UpdateMarketingPreferencesCommandHandler : ICommandHandler<UpdateMarketingPreferencesCommand>
{
    private readonly IMemberRepository _memberRepository;
    private readonly IMemberContactRepository _memberContactRepository;
    private readonly IEmailContactService _emailContactService;
    private readonly IConsentLogRepository _consentLogRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateMarketingPreferencesCommandHandler(
        IMemberRepository memberRepository,
        IMemberContactRepository memberContactRepository,
        IEmailContactService emailContactService,
        IConsentLogRepository consentLogRepository,
        IUnitOfWork unitOfWork)
    {
        _memberRepository = memberRepository;
        _memberContactRepository = memberContactRepository;
        _emailContactService = emailContactService;
        _consentLogRepository = consentLogRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> HandleAsync(UpdateMarketingPreferencesCommand command, CancellationToken cancellationToken = default)
    {
        var member = await _memberRepository.GetByIdAsync(command.MemberId, cancellationToken);
        if (member is null)
        {
            return Result.Failure(new Error("Member.NotFound", $"Member with ID '{command.MemberId}' was not found."));
        }

        var updateMemberResult = UpdateMemberMarketingPreferences(member, command, cancellationToken);
        if (updateMemberResult.IsFailure)
        {
            return updateMemberResult;
        }

        var consentLogResult = await LogConsentAsync(member, command, cancellationToken);
        if (consentLogResult.IsFailure)
        {
            return consentLogResult;
        }

        await SyncDownstreamSafelyAsync(member, command, cancellationToken);

        return await SaveChangesSafelyAsync(member, cancellationToken);
    }

    private Result UpdateMemberMarketingPreferences(
        Member member,
        UpdateMarketingPreferencesCommand command,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = member.UpdateMarketingPreferences(command.AcceptNews, command.AcceptPromo);
        if (result.IsFailure)
        {
            return result;
        }

        _memberRepository.Update(member);
        return Result.Success();
    }

    private async Task<Result> LogConsentAsync(
        Member member,
        UpdateMarketingPreferencesCommand command,
        CancellationToken cancellationToken)
    {
        var action = (command.AcceptNews || command.AcceptPromo) ? "Opt-In" : "Opt-Out";
        var consentLogResult = ConsentLog.Create(
            member.Id,
            action,
            command.IpAddress,
            command.Source,
            command.ConsentToken);

        if (consentLogResult.IsFailure)
        {
            return Result.Failure(consentLogResult.Error);
        }

        await _consentLogRepository.AddAsync(consentLogResult.Value, cancellationToken);
        return Result.Success();
    }

    private async Task SyncDownstreamSafelyAsync(
        Member member,
        UpdateMarketingPreferencesCommand command,
        CancellationToken cancellationToken)
    {
        bool syncFailed = false;
        Result? syncError = null;

        try
        {
            var syncResult = await SyncContactPreferencesAsync(member, command, cancellationToken);
            if (syncResult.IsFailure)
            {
                syncFailed = true;
                syncError = syncResult;
            }
        }
        catch (Exception ex)
        {
            syncFailed = true;
            syncError = Result.Failure(new Error("EmailContact.SyncException", $"An exception occurred during synchronization. Details: {ex.Message}"));
        }

        if (syncFailed)
        {
            member.MarkAsPendingSync();
            Console.Error.WriteLine($"[ALERT] Admin alert: Downstream marketing provider sync failed for Member ID '{member.Id}'. Logged as 'Pending Sync'. Error: {syncError?.Error.Message}");
        }
        else
        {
            member.ClearPendingSync();
        }
    }

    private async Task<Result> SaveChangesSafelyAsync(Member member, CancellationToken cancellationToken)
    {
        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[ERROR] Database save failed for Member ID '{member.Id}'. Details: {ex.Message}");
            return Result.Failure(new Error("Database.SaveFailed", "Failed to save marketing preferences locally."));
        }
    }

    private async Task<Result> SyncContactPreferencesAsync(
        Member member,
        UpdateMarketingPreferencesCommand command,
        CancellationToken cancellationToken)
    {
        var memberContact = await _memberContactRepository.GetByIdAsync(member.Id, cancellationToken);
        var anyPreferenceAccepted = command.AcceptNews || command.AcceptPromo;

        if (anyPreferenceAccepted)
        {
            return await SyncOptInAsync(member, memberContact, command, cancellationToken);
        }

        return await SyncOptOutAsync(member, memberContact, cancellationToken);
    }

    private async Task<Result> SyncOptInAsync(
        Member member,
        MemberContact? memberContact,
        UpdateMarketingPreferencesCommand command,
        CancellationToken cancellationToken)
    {
        if (memberContact is null || memberContact.ExternalContactId == "DELETED")
        {
            return await CreateOrRestoreContactAsync(member, memberContact, command, cancellationToken);
        }

        return await UpdateExistingContactAsync(member, memberContact, cancellationToken);
    }

    private async Task<Result> CreateOrRestoreContactAsync(
        Member member,
        MemberContact? memberContact,
        UpdateMarketingPreferencesCommand command,
        CancellationToken cancellationToken)
    {
        var resendResult = await _emailContactService.CreateContactAsync(
            member.Email,
            member.FirstName,
            member.LastName,
            command.AcceptNews,
            command.AcceptPromo,
            cancellationToken);

        if (resendResult.IsFailure)
        {
            return Result.Failure(resendResult.Error);
        }

        if (memberContact is null)
        {
            return await CreateNewMemberContactAsync(member, resendResult.Value, cancellationToken);
        }

        return RestoreExistingMemberContact(member, memberContact, resendResult.Value, cancellationToken);
    }

    private async Task<Result> CreateNewMemberContactAsync(
        Member member,
        string externalContactId,
        CancellationToken cancellationToken)
    {
        var newMemberContactResult = MemberContact.Create(
            member.Id,
            externalContactId,
            member.Email,
            phoneNumber: null);

        if (newMemberContactResult.IsFailure)
        {
            return Result.Failure(newMemberContactResult.Error);
        }

        var memberContact = newMemberContactResult.Value;
        UpdateSegments(memberContact, member.AcceptNews, member.AcceptPromo);

        await _memberContactRepository.AddAsync(memberContact, cancellationToken);
        return Result.Success();
    }

    private Result RestoreExistingMemberContact(
        Member member,
        MemberContact memberContact,
        string externalContactId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var updateResult = memberContact.UpdateContactDetails(externalContactId, member.Email);
        if (updateResult.IsFailure)
        {
            return updateResult;
        }

        UpdateSegments(memberContact, member.AcceptNews, member.AcceptPromo);
        _memberContactRepository.Update(memberContact);
        return Result.Success();
    }

    private async Task<Result> UpdateExistingContactAsync(
        Member member,
        MemberContact memberContact,
        CancellationToken cancellationToken)
    {
        UpdateSegments(memberContact, member.AcceptNews, member.AcceptPromo);
        _memberContactRepository.Update(memberContact);

        var updateResult = await _emailContactService.UpdateContactAsync(
            memberContact.ExternalContactId,
            member.FirstName,
            member.LastName,
            unsubscribed: false,
            cancellationToken);

        if (updateResult.IsFailure)
        {
            return Result.Failure(updateResult.Error);
        }

        return Result.Success();
    }

    private async Task<Result> SyncOptOutAsync(
        Member member,
        MemberContact? memberContact,
        CancellationToken cancellationToken)
    {
        if (memberContact is not null && memberContact.ExternalContactId != "DELETED")
        {
            var deleteResult = await _emailContactService.DeleteContactAsync(
                memberContact.ExternalContactId,
                cancellationToken);

            if (deleteResult.IsFailure)
            {
                return Result.Failure(deleteResult.Error);
            }

            var updateDetailsResult = memberContact.UpdateContactDetails("DELETED", memberContact.Email);
            if (updateDetailsResult.IsFailure)
            {
                return updateDetailsResult;
            }

            UnsubscribeAllSegments(memberContact);
            _memberContactRepository.Update(memberContact);
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
