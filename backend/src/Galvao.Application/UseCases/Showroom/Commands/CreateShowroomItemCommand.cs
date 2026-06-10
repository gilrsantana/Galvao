using Galvao.Application.Common.CQRS;

namespace Galvao.Application.UseCases.Showroom.Commands;

public record CreateShowroomItemCommand(
    string Title,
    string Description,
    decimal Price,
    string Category) : ICommand<Guid>;
