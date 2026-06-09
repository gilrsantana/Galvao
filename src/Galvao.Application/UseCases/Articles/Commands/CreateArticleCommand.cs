using Galvao.Application.Common.CQRS;

namespace Galvao.Application.UseCases.Articles.Commands;

public record CreateArticleCommand(
    string Title,
    string Content,
    string Author) : ICommand<Guid>;
