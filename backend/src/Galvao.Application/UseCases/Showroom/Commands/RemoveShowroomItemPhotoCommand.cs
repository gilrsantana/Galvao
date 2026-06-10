using Galvao.Application.Common.CQRS;

namespace Galvao.Application.UseCases.Showroom.Commands;

public record RemoveShowroomItemPhotoCommand(
    Guid ShowroomItemId,
    Guid PhotoId) : ICommand;
