using Galvao.Application.Common.CQRS;

namespace Galvao.Application.UseCases.Articles.Queries;

public record GetArticleBySlugQuery(string Slug) : IQuery<ArticleResponse>;
