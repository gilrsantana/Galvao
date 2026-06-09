using Galvao.Application.Common.CQRS;
using Galvao.Application.Common.Interfaces;
using Galvao.Application.UseCases.Members.Commands;
using Galvao.Shared;

namespace Galvao.Application.UseCases.Members.CommandHandlers;

public class UpdateMemberProfileCommandHandler : ICommandHandler<UpdateMemberProfileCommand>
{
    private readonly IMemberRepository _memberRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateMemberProfileCommandHandler(IMemberRepository memberRepository, IUnitOfWork unitOfWork)
    {
        _memberRepository = memberRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> HandleAsync(UpdateMemberProfileCommand command, CancellationToken cancellationToken = default)
    {
        var member = await _memberRepository.GetByIdAsync(command.MemberId, cancellationToken);
        if (member is null)
        {
            return Result.Failure(new Error("Member.NotFound", $"Member with ID '{command.MemberId}' was not found."));
        }

        var result = member.UpdateProfile(command.DisplayName, command.FirstName, command.LastName);
        if (result.IsFailure)
        {
            return Result.Failure(result.Error);
        }

        _memberRepository.Update(member);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
