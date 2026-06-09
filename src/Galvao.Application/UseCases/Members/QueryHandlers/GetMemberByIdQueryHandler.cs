using Galvao.Application.Common.CQRS;
using Galvao.Application.Common.Interfaces;
using Galvao.Application.UseCases.Members.Queries;
using Galvao.Shared;

namespace Galvao.Application.UseCases.Members.QueryHandlers;

public class GetMemberByIdQueryHandler : IQueryHandler<GetMemberByIdQuery, MemberResponse>
{
    private readonly IMemberRepository _memberRepository;

    public GetMemberByIdQueryHandler(IMemberRepository memberRepository)
    {
        _memberRepository = memberRepository;
    }

    public async Task<Result<MemberResponse>> HandleAsync(GetMemberByIdQuery query, CancellationToken cancellationToken = default)
    {
        var member = await _memberRepository.GetByIdAsync(query.MemberId, cancellationToken);
        if (member is null)
        {
            return Result.Failure<MemberResponse>(new Error("Member.NotFound", $"Member with ID '{query.MemberId}' was not found."));
        }

        var response = new MemberResponse(
            member.Id,
            member.Email,
            member.DisplayName,
            member.FirstName,
            member.LastName,
            member.Active,
            member.CreatedAt,
            member.UpdatedAt);

        return response; // Implicit conversion to Result<MemberResponse>
    }
}
