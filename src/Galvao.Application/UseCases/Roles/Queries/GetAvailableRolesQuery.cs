using Galvao.Application.Common.CQRS;
using Galvao.Application.Common.Interfaces;

namespace Galvao.Application.UseCases.Roles.Queries;

public record GetAvailableRolesQuery() : IQuery<List<RoleResponse>>;
