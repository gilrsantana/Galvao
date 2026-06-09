using Galvao.Application.Common.CQRS;

namespace Galvao.Application.UseCases.Members.Queries;

public record MemberResponse(Guid Id, string Email, string DisplayName, string FirstName, string LastName, bool Active, DateTime CreatedAt, DateTime? UpdatedAt);

public record GetMemberByIdQuery(Guid MemberId) : IQuery<MemberResponse>;
