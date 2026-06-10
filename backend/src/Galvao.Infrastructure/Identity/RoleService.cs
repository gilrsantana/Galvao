using Galvao.Application.Common.Interfaces;
using Galvao.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Galvao.Infrastructure.Identity;

public class RoleService : IRoleService
{
    private readonly UserManager<Account> _userManager;
    private readonly RoleManager<Role> _roleManager;

    public RoleService(
        UserManager<Account> userManager,
        RoleManager<Role> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<Result> AssignRoleAsync(Guid userId, string roleName, CancellationToken cancellationToken = default)
    {
        var account = await _userManager.FindByIdAsync(userId.ToString());
        if (account is null)
            return Result.Failure(new Error("Auth.UserNotFound", $"User with ID '{userId}' was not found."));

        var roleExists = await _roleManager.RoleExistsAsync(roleName);
        if (!roleExists)
            return Result.Failure(new Error("Auth.RoleNotFound", $"Role '{roleName}' does not exist."));

        var result = await _userManager.AddToRoleAsync(account, roleName);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            return Result.Failure(new Error("Auth.RoleAssignmentFailed", errors));
        }

        return Result.Success();
    }

    public async Task<Result> RemoveRoleAsync(Guid userId, string roleName, CancellationToken cancellationToken = default)
    {
        var account = await _userManager.FindByIdAsync(userId.ToString());
        if (account is null)
            return Result.Failure(new Error("Auth.UserNotFound", $"User with ID '{userId}' was not found."));

        var hasRole = await _userManager.IsInRoleAsync(account, roleName);
        if (!hasRole)
            return Result.Failure(new Error("Auth.UserRoleNotFound", $"User does not have role '{roleName}'."));

        var result = await _userManager.RemoveFromRoleAsync(account, roleName);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            return Result.Failure(new Error("Auth.RoleRemovalFailed", errors));
        }

        return Result.Success();
    }

    public async Task<Result<List<string>>> GetUserRolesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var account = await _userManager.FindByIdAsync(userId.ToString());
        if (account is null)
            return Result.Failure<List<string>>(new Error("Auth.UserNotFound", $"User with ID '{userId}' was not found."));

        var roles = await _userManager.GetRolesAsync(account);
        return roles.ToList();
    }

    public async Task<Result<List<RoleResponse>>> GetAvailableRolesAsync(CancellationToken cancellationToken = default)
    {
        var roles = await _roleManager.Roles
            .Select(r => new RoleResponse(r.Id, r.Name!, r.Description))
            .ToListAsync(cancellationToken);

        return roles;
    }

    public async Task<Result> CreateRoleAsync(string roleName, string description, CancellationToken cancellationToken = default)
    {
        var roleExists = await _roleManager.RoleExistsAsync(roleName);
        if (roleExists)
            return Result.Failure(new Error("Auth.RoleAlreadyExists", $"Role '{roleName}' already exists."));

        var role = Role.Create(roleName, description);
        var result = await _roleManager.CreateAsync(role);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            return Result.Failure(new Error("Auth.RoleCreationFailed", errors));
        }

        return Result.Success();
    }
}
