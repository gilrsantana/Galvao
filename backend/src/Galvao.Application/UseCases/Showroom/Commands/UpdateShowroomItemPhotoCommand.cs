using Galvao.Application.Common.CQRS;

namespace Galvao.Application.UseCases.Showroom.Commands;

public record UpdateShowroomItemPhotoCommand(
    Guid ShowroomItemId,
    Guid PhotoId,
    string Url,
    string Caption,
    bool IsPrimary) : ICommand;
