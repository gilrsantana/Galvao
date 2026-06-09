using Galvao.Application.Common.CQRS;

namespace Galvao.Application.UseCases.Showroom.Commands;

public record AddShowroomItemPhotoCommand(
    Guid ShowroomItemId,
    string Url,
    string Caption,
    bool IsPrimary) : ICommand<Guid>;
