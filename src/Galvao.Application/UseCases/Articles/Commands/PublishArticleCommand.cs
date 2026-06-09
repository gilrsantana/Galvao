using Galvao.Application.Common.CQRS;

namespace Galvao.Application.UseCases.Articles.Commands;

public record PublishArticleCommand(
    Guid Id,
    bool Publish) : ICommand;
