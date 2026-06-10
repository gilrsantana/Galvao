using Galvao.Application.Common.CQRS;
using Galvao.Application.UseCases.Members.Commands;
using Galvao.Application.UseCases.Members.Queries;
using Galvao.Presentation.Requests.Members;
using Microsoft.AspNetCore.Mvc;

namespace Galvao.Presentation.Controllers;

public class MembersController : ApiControllerBase
{
    private readonly IQueryHandler<GetMemberByIdQuery, MemberResponse> _getMemberByIdHandler;
    private readonly ICommandHandler<UpdateMemberProfileCommand> _updateProfileHandler;
    private readonly ICommandHandler<ChangeEmailCommand> _changeEmailHandler;
    private readonly ICommandHandler<ChangePasswordCommand> _changePasswordHandler;
    private readonly ICommandHandler<UpdateMarketingPreferencesCommand> _updatePreferencesHandler;

    public MembersController(
        IQueryHandler<GetMemberByIdQuery, MemberResponse> getMemberByIdHandler,
        ICommandHandler<UpdateMemberProfileCommand> updateProfileHandler,
        ICommandHandler<ChangeEmailCommand> changeEmailHandler,
        ICommandHandler<ChangePasswordCommand> changePasswordHandler,
        ICommandHandler<UpdateMarketingPreferencesCommand> updatePreferencesHandler)
    {
        _getMemberByIdHandler = getMemberByIdHandler;
        _updateProfileHandler = updateProfileHandler;
        _changeEmailHandler = changeEmailHandler;
        _changePasswordHandler = changePasswordHandler;
        _updatePreferencesHandler = updatePreferencesHandler;
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(MemberResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _getMemberByIdHandler.HandleAsync(new GetMemberByIdQuery(id), cancellationToken);
        return HandleResult(result);
    }

    [HttpPut("{id:guid}/profile")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateProfile(Guid id, [FromBody] UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        var result = await _updateProfileHandler.HandleAsync(new UpdateMemberProfileCommand(id, request.DisplayName, request.FirstName, request.LastName), cancellationToken);
        return HandleResult(result);
    }

    [HttpPut("{id:guid}/email")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangeEmail(Guid id, [FromBody] ChangeEmailRequest request, CancellationToken cancellationToken)
    {
        var result = await _changeEmailHandler.HandleAsync(new ChangeEmailCommand(id, request.NewEmail), cancellationToken);
        return HandleResult(result);
    }

    [HttpPut("{id:guid}/password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword(Guid id, [FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var result = await _changePasswordHandler.HandleAsync(new ChangePasswordCommand(id, request.CurrentPassword, request.NewPassword), cancellationToken);
        return HandleResult(result);
    }

    [HttpPut("{id:guid}/preferences")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdatePreferences(Guid id, [FromBody] UpdateMarketingPreferencesRequest request, CancellationToken cancellationToken)
    {
        var result = await _updatePreferencesHandler.HandleAsync(new UpdateMarketingPreferencesCommand(id, request.AcceptNews, request.AcceptPromo), cancellationToken);
        return HandleResult(result);
    }
}
