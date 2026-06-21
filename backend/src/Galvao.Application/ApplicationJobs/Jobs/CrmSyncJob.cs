using Galvao.Application.Common.Interfaces;
using Galvao.Application.Jobs.Interfaces;
using Galvao.Domain.MemberContactAggregate.Entities;
using Galvao.Domain.MemberContactAggregate.Enums;

namespace Galvao.Application.ApplicationJobs.Jobs;

public class CrmSyncJob(
    IEmailContactService emailContactService,
    IMemberContactRepository memberContactRepository,
    IUnitOfWork unitOfWork) : ICrmSyncJob
{
    private readonly IEmailContactService _emailContactService = emailContactService;
    private readonly IMemberContactRepository _memberContactRepository = memberContactRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task SyncContactAsync(
        Guid userId,
        string email,
        string firstName,
        string lastName,
        bool acceptNews,
        bool acceptPromo,
        CancellationToken cancellationToken = default)
    {
        if (!acceptNews && !acceptPromo)
        {
            return;
        }

        var resendResult = await _emailContactService.CreateContactAsync(
            email,
            firstName,
            lastName,
            acceptNews,
            acceptPromo,
            cancellationToken);

        if (resendResult.IsFailure)
        {
            throw new InvalidOperationException($"Resend API call failed: {resendResult.Error.Message} (Code: {resendResult.Error.Code})");
        }

        var memberContactResult = MemberContact.Create(userId, resendResult.Value, email, phoneNumber: null);
        if (memberContactResult.IsFailure)
        {
            throw new InvalidOperationException($"Failed to create MemberContact domain model: {memberContactResult.Error.Message} (Code: {memberContactResult.Error.Code})");
        }

        var memberContact = memberContactResult.Value;

        if (acceptNews)
        {
            var segment = memberContact.AddEmailSegment(ESegmentType.News);
            if (segment.IsSuccess)
            {
                memberContact.SetEmailSegmentSubscriptionDate(ESegmentType.News);
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

        await _memberContactRepository.AddAsync(memberContact, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
