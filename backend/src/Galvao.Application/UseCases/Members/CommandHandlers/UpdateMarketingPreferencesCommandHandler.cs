using System;
using System.Threading;
using System.Threading.Tasks;
using Galvao.Application.Common.CQRS;
using Galvao.Application.Common.Interfaces;
using Galvao.Application.UseCases.Members.Commands;
using Galvao.Domain.Entities;
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

        var updateMemberResult = UpdateMemberMarketingPreferences(member, command);
        if (updateMemberResult.IsFailure)
        {
            return updateMemberResult;
        }

        // Determine Action for Consent Logging
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

        // Perform Downstream Synchronization safely
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
            // Prevent PII leak in error message
            syncError = Result.Failure(new Error("EmailContact.SyncException", $"An exception occurred during synchronization. Details: {ex.Message}"));
        }

        if (syncFailed)
        {
            // Flag user profile as Pending Sync and log alert for admins
            member.MarkAsPendingSync();
            Console.Error.WriteLine($"[ALERT] Admin alert: Downstream marketing provider sync failed for Member ID '{member.Id}'. Logged as 'Pending Sync'. Error: {syncError?.Error.Message}");
        }
        else
        {
            member.ClearPendingSync();
        }

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // Prevent raw PII or database details from being leaked in plain text error logs
            Console.Error.WriteLine($"[ERROR] Database save failed for Member ID '{member.Id}'. Details: {ex.Message}");
            return Result.Failure(new Error("Database.SaveFailed", "Failed to save marketing preferences locally."));
        }

        return Result.Success();
    }

    private Result UpdateMemberMarketingPreferences(Member member, UpdateMarketingPreferencesCommand command)
    {
        var result = member.UpdateMarketingPreferences(command.AcceptNews, command.AcceptPromo);
        if (result.IsFailure)
        {
            return result;
        }

        _memberRepository.Update(member);
        return Result.Success();
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
            return await CreateNewMemberContactAsync(member.Id, member.Email, resendResult.Value, cancellationToken);
        }

        return RestoreExistingMemberContact(memberContact, resendResult.Value, member.Email);
    }

    private async Task<Result> CreateNewMemberContactAsync(
        Guid memberId,
        string email,
        string externalContactId,
        CancellationToken cancellationToken)
    {
        var newMemberContactResult = MemberContact.Create(
            memberId,
            externalContactId,
            email,
            unsubscribed: false);

        if (newMemberContactResult.IsFailure)
        {
            return Result.Failure(newMemberContactResult.Error);
        }

        await _memberContactRepository.AddAsync(newMemberContactResult.Value, cancellationToken);
        return Result.Success();
    }

    private Result RestoreExistingMemberContact(MemberContact memberContact, string externalContactId, string email)
    {
        var updateResult = memberContact.UpdateContactDetails(externalContactId, email);
        if (updateResult.IsFailure)
        {
            return updateResult;
        }

        memberContact.UpdateStatus(false);
        _memberContactRepository.Update(memberContact);
        return Result.Success();
    }

    private async Task<Result> UpdateExistingContactAsync(
        Member member,
        MemberContact memberContact,
        CancellationToken cancellationToken)
    {
        memberContact.UpdateStatus(false);
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

            memberContact.UpdateStatus(true);
            _memberContactRepository.Update(memberContact);
        }

        return Result.Success();
    }
}
