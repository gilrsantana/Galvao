using Galvao.Application.Common.CQRS;
using Galvao.Application.Common.Interfaces;
using Galvao.Application.UseCases.Roles.Commands;
using Galvao.Application.UseCases.Roles.Queries;
using Galvao.Presentation.Requests.Roles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Galvao.Presentation.Controllers;

public class RolesController : ApiControllerBase
{
    private readonly ICommandHandler<CreateRoleCommand> _createRoleHandler;
    private readonly ICommandHandler<AssignRoleCommand> _assignRoleHandler;
    private readonly ICommandHandler<RemoveRoleCommand> _removeRoleHandler;
    private readonly IQueryHandler<GetUserRolesQuery, List<string>> _getUserRolesHandler;
    private readonly IQueryHandler<GetAvailableRolesQuery, List<RoleResponse>> _getAvailableRolesHandler;

    public RolesController(
        ICommandHandler<CreateRoleCommand> createRoleHandler,
        ICommandHandler<AssignRoleCommand> assignRoleHandler,
        ICommandHandler<RemoveRoleCommand> removeRoleHandler,
        IQueryHandler<GetUserRolesQuery, List<string>> getUserRolesHandler,
        IQueryHandler<GetAvailableRolesQuery, List<RoleResponse>> getAvailableRolesHandler)
    {
        _createRoleHandler = createRoleHandler;
        _assignRoleHandler = assignRoleHandler;
        _removeRoleHandler = removeRoleHandler;
        _getUserRolesHandler = getUserRolesHandler;
        _getAvailableRolesHandler = getAvailableRolesHandler;
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create([FromBody] CreateRoleRequest request, CancellationToken cancellationToken)
    {
        var result = await _createRoleHandler.HandleAsync(
            new CreateRoleCommand(request.RoleName, request.Description), 
            cancellationToken);
        return HandleResult(result);
    }

    [HttpPost("assign")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Assign([FromBody] AssignRoleRequest request, CancellationToken cancellationToken)
    {
        var result = await _assignRoleHandler.HandleAsync(
            new AssignRoleCommand(request.UserId, request.RoleName), 
            cancellationToken);
        return HandleResult(result);
    }

    [HttpPost("remove")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Remove([FromBody] AssignRoleRequest request, CancellationToken cancellationToken)
    {
        var result = await _removeRoleHandler.HandleAsync(
            new RemoveRoleCommand(request.UserId, request.RoleName), 
            cancellationToken);
        return HandleResult(result);
    }

    [HttpGet("user/{userId:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetUserRoles(Guid userId, CancellationToken cancellationToken)
    {
        var result = await _getUserRolesHandler.HandleAsync(new GetUserRolesQuery(userId), cancellationToken);
        return HandleResult(result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<RoleResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAvailableRoles(CancellationToken cancellationToken)
    {
        var result = await _getAvailableRolesHandler.HandleAsync(new GetAvailableRolesQuery(), cancellationToken);
        return HandleResult(result);
    }
}
