using Galvao.Application.Common.CQRS;
using Galvao.Application.Common.Interfaces;
using Galvao.Application.UseCases.Articles.Commands;
using Galvao.Domain.ArticleAggregate.Entities;
using Galvao.Shared;

namespace Galvao.Application.UseCases.Articles.CommandHandlers;

public class UpdateArticleCommandHandler : ICommandHandler<UpdateArticleCommand>
{
    private readonly IArticleRepository _articleRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateArticleCommandHandler(
        IArticleRepository articleRepository,
        IUnitOfWork unitOfWork)
    {
        _articleRepository = articleRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> HandleAsync(UpdateArticleCommand command, CancellationToken cancellationToken = default)
    {
        var article = await _articleRepository.GetByIdAsync(command.Id, cancellationToken);
        if (article is null)
            return Result.Failure(new Error("Article.NotFound", $"Article with ID '{command.Id}' was not found."));

        var slug = Article.Slugify(command.Title);
        var existingArticle = await _articleRepository.GetBySlugAsync(slug, cancellationToken);
        if (existingArticle is not null && existingArticle.Id != command.Id)
        {
            return Result.Failure(new Error("Article.SlugExists", "Um artigo com o mesmo título ou slug já existe."));
        }

        var updateResult = article.UpdateContent(command.Title, command.Content, command.Author);
        if (updateResult.IsFailure)
            return updateResult;

        _articleRepository.Update(article);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
