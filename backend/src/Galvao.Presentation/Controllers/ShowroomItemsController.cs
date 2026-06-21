using Galvao.Application.Common.CQRS;
using Galvao.Application.UseCases.Showroom.Commands;
using Galvao.Application.UseCases.Showroom.Queries;
using Galvao.Presentation.Requests.Showroom;
using Galvao.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Galvao.Presentation.Controllers;

public class ShowroomItemsController : ApiControllerBase
{
    private readonly ICommandHandler<CreateShowroomItemCommand, Guid> _createHandler;
    private readonly ICommandHandler<UpdateShowroomItemCommand> _updateHandler;
    private readonly ICommandHandler<AddShowroomItemPhotoCommand, Guid> _addPhotoHandler;
    private readonly ICommandHandler<RemoveShowroomItemPhotoCommand> _removePhotoHandler;
    private readonly ICommandHandler<UpdateShowroomItemPhotoCommand> _updatePhotoHandler;
    private readonly IQueryHandler<GetShowroomItemByIdQuery, ShowroomItemResponse> _getByIdHandler;
    private readonly IQueryHandler<GetPagedShowroomItemsQuery, PagedResponse<ShowroomItemResponse>> _getPagedHandler;

    public ShowroomItemsController(
        ICommandHandler<CreateShowroomItemCommand, Guid> createHandler,
        ICommandHandler<UpdateShowroomItemCommand> updateHandler,
        ICommandHandler<AddShowroomItemPhotoCommand, Guid> addPhotoHandler,
        ICommandHandler<RemoveShowroomItemPhotoCommand> removePhotoHandler,
        ICommandHandler<UpdateShowroomItemPhotoCommand> updatePhotoHandler,
        IQueryHandler<GetShowroomItemByIdQuery, ShowroomItemResponse> getByIdHandler,
        IQueryHandler<GetPagedShowroomItemsQuery, PagedResponse<ShowroomItemResponse>> getPagedHandler)
    {
        _createHandler = createHandler;
        _updateHandler = updateHandler;
        _addPhotoHandler = addPhotoHandler;
        _removePhotoHandler = removePhotoHandler;
        _updatePhotoHandler = updatePhotoHandler;
        _getByIdHandler = getByIdHandler;
        _getPagedHandler = getPagedHandler;
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create([FromBody] CreateShowroomItemRequest request, CancellationToken cancellationToken)
    {
        var result = await _createHandler.HandleAsync(
            new CreateShowroomItemCommand(request.Title, request.Description, request.Price, request.Category),
            cancellationToken);
        return HandleResult(result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateShowroomItemRequest request, CancellationToken cancellationToken)
    {
        var result = await _updateHandler.HandleAsync(
            new UpdateShowroomItemCommand(id, request.Title, request.Description, request.Price, request.Category),
            cancellationToken);
        return HandleResult(result);
    }

    [HttpPost("{id:guid}/photos")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> AddPhoto(Guid id, [FromBody] AddShowroomItemPhotoRequest request, CancellationToken cancellationToken)
    {
        var result = await _addPhotoHandler.HandleAsync(
            new AddShowroomItemPhotoCommand(id, request.Url, request.Caption, request.IsPrimary),
            cancellationToken);
        return HandleResult(result);
    }

    [HttpDelete("{id:guid}/photos/{photoId:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RemovePhoto(Guid id, Guid photoId, CancellationToken cancellationToken)
    {
        var result = await _removePhotoHandler.HandleAsync(
            new RemoveShowroomItemPhotoCommand(id, photoId),
            cancellationToken);
        return HandleResult(result);
    }

    [HttpPut("{id:guid}/photos/{photoId:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdatePhoto(Guid id, Guid photoId, [FromBody] UpdateShowroomItemPhotoRequest request, CancellationToken cancellationToken)
    {
        var result = await _updatePhotoHandler.HandleAsync(
            new UpdateShowroomItemPhotoCommand(id, photoId, request.Url, request.Caption, request.IsPrimary),
            cancellationToken);
        return HandleResult(result);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ShowroomItemResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _getByIdHandler.HandleAsync(new GetShowroomItemByIdQuery(id), cancellationToken);
        return HandleResult(result);
    }

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResponse<ShowroomItemResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var result = await _getPagedHandler.HandleAsync(new GetPagedShowroomItemsQuery(page, pageSize), cancellationToken);
        return HandleResult(result);
    }
}
