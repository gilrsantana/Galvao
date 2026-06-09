using Galvao.Application.Common.CQRS;

namespace Galvao.Application.UseCases.Articles.Commands;

public record UpdateArticleCommand(
    Guid Id,
    string Title,
    string Content,
    string Author) : ICommand;
