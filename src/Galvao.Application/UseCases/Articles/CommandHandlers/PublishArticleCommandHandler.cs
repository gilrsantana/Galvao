using Galvao.Application.Common.CQRS;
using Galvao.Application.Common.Interfaces;
using Galvao.Application.UseCases.Articles.Commands;
using Galvao.Shared;

namespace Galvao.Application.UseCases.Articles.CommandHandlers;

public class PublishArticleCommandHandler : ICommandHandler<PublishArticleCommand>
{
    private readonly IArticleRepository _articleRepository;
    private readonly IUnitOfWork _unitOfWork;

    public PublishArticleCommandHandler(
        IArticleRepository articleRepository,
        IUnitOfWork unitOfWork)
    {
        _articleRepository = articleRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> HandleAsync(PublishArticleCommand command, CancellationToken cancellationToken = default)
    {
        var article = await _articleRepository.GetByIdAsync(command.Id, cancellationToken);
        if (article is null)
            return Result.Failure(new Error("Article.NotFound", $"Article with ID '{command.Id}' was not found."));

        var publishResult = command.Publish ? article.Publish() : article.Unpublish();
        if (publishResult.IsFailure)
            return publishResult;

        _articleRepository.Update(article);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
