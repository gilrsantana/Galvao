using Galvao.Application.Common.CQRS;
using Galvao.Application.UseCases.Articles.Commands;
using Galvao.Application.UseCases.Articles.Queries;
using Galvao.Presentation.Requests.Articles;
using Galvao.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Galvao.Presentation.Controllers;

public class ArticlesController : ApiControllerBase
{
    private readonly ICommandHandler<CreateArticleCommand, Guid> _createHandler;
    private readonly ICommandHandler<UpdateArticleCommand> _updateHandler;
    private readonly ICommandHandler<PublishArticleCommand> _publishHandler;
    private readonly IQueryHandler<GetArticleByIdQuery, ArticleResponse> _getByIdHandler;
    private readonly IQueryHandler<GetPagedArticlesQuery, PagedResponse<ArticleResponse>> _getPagedHandler;

    public ArticlesController(
        ICommandHandler<CreateArticleCommand, Guid> createHandler,
        ICommandHandler<UpdateArticleCommand> updateHandler,
        ICommandHandler<PublishArticleCommand> publishHandler,
        IQueryHandler<GetArticleByIdQuery, ArticleResponse> getByIdHandler,
        IQueryHandler<GetPagedArticlesQuery, PagedResponse<ArticleResponse>> getPagedHandler)
    {
        _createHandler = createHandler;
        _updateHandler = updateHandler;
        _publishHandler = publishHandler;
        _getByIdHandler = getByIdHandler;
        _getPagedHandler = getPagedHandler;
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create([FromBody] CreateArticleRequest request, CancellationToken cancellationToken)
    {
        var result = await _createHandler.HandleAsync(
            new CreateArticleCommand(request.Title, request.Content, request.Author),
            cancellationToken);
        return HandleResult(result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateArticleRequest request, CancellationToken cancellationToken)
    {
        var result = await _updateHandler.HandleAsync(
            new UpdateArticleCommand(id, request.Title, request.Content, request.Author),
            cancellationToken);
        return HandleResult(result);
    }

    [HttpPut("{id:guid}/publish")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Publish(Guid id, [FromBody] PublishArticleRequest request, CancellationToken cancellationToken)
    {
        var result = await _publishHandler.HandleAsync(
            new PublishArticleCommand(id, request.Publish),
            cancellationToken);
        return HandleResult(result);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ArticleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _getByIdHandler.HandleAsync(new GetArticleByIdQuery(id), cancellationToken);
        return HandleResult(result);
    }

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResponse<ArticleResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] bool onlyPublished = true,
        CancellationToken cancellationToken = default)
    {
        // Enforce onlyPublished = true for non-Admins
        var isAdmin = User?.IsInRole("Admin") ?? false;
        var fetchOnlyPublished = !isAdmin || onlyPublished;

        var result = await _getPagedHandler.HandleAsync(
            new GetPagedArticlesQuery(page, pageSize, fetchOnlyPublished),
            cancellationToken);
        return HandleResult(result);
    }
}
