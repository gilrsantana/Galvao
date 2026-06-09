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

    public MembersController(
        IQueryHandler<GetMemberByIdQuery, MemberResponse> getMemberByIdHandler,
        ICommandHandler<UpdateMemberProfileCommand> updateProfileHandler)
    {
        _getMemberByIdHandler = getMemberByIdHandler;
        _updateProfileHandler = updateProfileHandler;
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
}
