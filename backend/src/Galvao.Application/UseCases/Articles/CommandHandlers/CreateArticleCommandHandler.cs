using Galvao.Application.Common.CQRS;
using Galvao.Application.Common.Interfaces;
using Galvao.Application.UseCases.Articles.Commands;
using Galvao.Domain.ArticleAggregate.Entities;
using Galvao.Shared;

namespace Galvao.Application.UseCases.Articles.CommandHandlers;

public class CreateArticleCommandHandler : ICommandHandler<CreateArticleCommand, Guid>
{
    private readonly IArticleRepository _articleRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateArticleCommandHandler(
        IArticleRepository articleRepository,
        IUnitOfWork unitOfWork)
    {
        _articleRepository = articleRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> HandleAsync(CreateArticleCommand command, CancellationToken cancellationToken = default)
    {
        var slug = Article.Slugify(command.Title);
        var existingArticle = await _articleRepository.GetBySlugAsync(slug, cancellationToken);
        if (existingArticle is not null)
        {
            return Result.Failure<Guid>(new Error("Article.SlugExists", "Um artigo com o mesmo título ou slug já existe."));
        }

        var articleResult = Article.Create(command.Title, command.Content, command.Author);
        if (articleResult.IsFailure)
            return Result.Failure<Guid>(articleResult.Error);

        var article = articleResult.Value;

        await _articleRepository.AddAsync(article, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return article.Id;
    }
}
