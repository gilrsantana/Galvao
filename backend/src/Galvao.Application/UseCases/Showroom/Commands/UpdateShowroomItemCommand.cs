using Galvao.Application.Common.CQRS;

namespace Galvao.Application.UseCases.Showroom.Commands;

public record UpdateShowroomItemCommand(
    Guid Id,
    string Title,
    string Description,
    decimal Price,
    string Category) : ICommand;
