using System;
using System.Threading;
using System.Threading.Tasks;
using Galvao.Application.Common.Interfaces;
using Galvao.Domain.Entities;
using Galvao.Shared;

namespace Galvao.Infrastructure.Identity.Jobs;

public class CrmSyncJob : ICrmSyncJob
{
    private readonly IEmailContactService _emailContactService;
    private readonly IMemberContactRepository _memberContactRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CrmSyncJob(
        IEmailContactService emailContactService,
        IMemberContactRepository memberContactRepository,
        IUnitOfWork unitOfWork)
    {
        _emailContactService = emailContactService;
        _memberContactRepository = memberContactRepository;
        _unitOfWork = unitOfWork;
    }

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

        var memberContactResult = MemberContact.Create(userId, resendResult.Value, email, unsubscribed: false);
        if (memberContactResult.IsFailure)
        {
            throw new InvalidOperationException($"Failed to create MemberContact domain model: {memberContactResult.Error.Message} (Code: {memberContactResult.Error.Code})");
        }

        await _memberContactRepository.AddAsync(memberContactResult.Value, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
