using Galvao.Shared;

namespace Galvao.Application.Common.Interfaces;

public record RoleResponse(Guid Id, string Name, string Description);

public interface IRoleService
{
    Task<Result> AssignRoleAsync(Guid userId, string roleName, CancellationToken cancellationToken = default);
    Task<Result> RemoveRoleAsync(Guid userId, string roleName, CancellationToken cancellationToken = default);
    Task<Result<List<string>>> GetUserRolesAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Result<List<RoleResponse>>> GetAvailableRolesAsync(CancellationToken cancellationToken = default);
    Task<Result> CreateRoleAsync(string roleName, string description, CancellationToken cancellationToken = default);
}
