using Galvao.Application.Common.CQRS;

namespace Galvao.Application.UseCases.Articles.Queries;

public record GetArticleByIdQuery(Guid ArticleId) : IQuery<ArticleResponse>;
