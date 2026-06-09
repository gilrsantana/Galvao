using Galvao.Application.Common.CQRS;

namespace Galvao.Application.UseCases.Roles.Queries;

public record GetUserRolesQuery(Guid UserId) : IQuery<List<string>>;
