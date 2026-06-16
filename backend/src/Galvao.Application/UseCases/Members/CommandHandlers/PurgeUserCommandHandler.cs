using Galvao.Application.Common.CQRS;
using Galvao.Application.Common.Interfaces;
using Galvao.Application.UseCases.Members.Commands;
using Galvao.Domain.Entities;
using Galvao.Shared;

namespace Galvao.Application.UseCases.Members.CommandHandlers;

public class PurgeUserCommandHandler : ICommandHandler<PurgeUserCommand>
{
    private readonly IMemberRepository _memberRepository;
    private readonly IMemberContactRepository _memberContactRepository;
    private readonly IRemovedUserRepository _removedUserRepository;
    private readonly IEmailContactService _emailContactService;
    private readonly IIdentityService _identityService;
    private readonly IUnitOfWork _unitOfWork;

    public PurgeUserCommandHandler(
        IMemberRepository memberRepository,
        IMemberContactRepository memberContactRepository,
        IRemovedUserRepository removedUserRepository,
        IEmailContactService emailContactService,
        IIdentityService identityService,
        IUnitOfWork unitOfWork)
    {
        _memberRepository = memberRepository;
        _memberContactRepository = memberContactRepository;
        _removedUserRepository = removedUserRepository;
        _emailContactService = emailContactService;
        _identityService = identityService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> HandleAsync(PurgeUserCommand command, CancellationToken cancellationToken = default)
    {
        var member = await _memberRepository.GetByIdAsync(command.MemberId, cancellationToken);
        if (member is null)
        {
            return Result.Failure(new Error("Member.NotFound", $"Member with ID '{command.MemberId}' was not found."));
        }

        var checkPasswordResult = await _identityService.CheckPasswordAsync(member.Id, command.Password, cancellationToken);
        if (checkPasswordResult.IsFailure)
        {
            return Result.Failure(checkPasswordResult.Error);
        }

        var fullName = $"{member.FirstName} {member.LastName}".Trim();
        var email = member.Email;

        bool removedPersonalInformation = false;
        bool removedAccountData = false;
        bool removedMarketData = false;
        bool removedFromMailProvider = false;

        // 1. Delete contact in external email marketing provider (Resend)
        var memberContact = await _memberContactRepository.GetByIdAsync(member.Id, cancellationToken);
        if (memberContact is not null)
        {
            var deleteContactResult = await _emailContactService.DeleteContactAsync(memberContact.ExternalContactId, cancellationToken);
            if (deleteContactResult.IsSuccess)
            {
                removedFromMailProvider = true;
            }

            _memberContactRepository.Remove(memberContact);
            removedMarketData = true;
        }
        else
        {
            removedMarketData = true;
            removedFromMailProvider = true;
        }

        // 2. Remove member locally from database
        _memberRepository.Remove(member);
        removedPersonalInformation = true;

        // 3. Delete user account from Identity system (includes password check)
        var deleteAccountResult = await _identityService.DeleteAccountAsync(member.Id, command.Password, cancellationToken);
        if (deleteAccountResult.IsFailure)
        {
            return Result.Failure(deleteAccountResult.Error);
        }
        removedAccountData = true;

        // 4. Create and persist RemovedUser audit log entity
        var removedUserResult = RemovedUser.Create(
            fullName,
            email,
            removedPersonalInformation,
            removedAccountData,
            removedMarketData,
            removedFromMailProvider);

        if (removedUserResult.IsFailure)
        {
            return Result.Failure(removedUserResult.Error);
        }

        await _removedUserRepository.AddAsync(removedUserResult.Value, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
