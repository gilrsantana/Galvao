using Galvao.Application.Common.CQRS;
using Galvao.Application.Common.Interfaces;
using Galvao.Application.UseCases.Showroom.Commands;
using Galvao.Shared;

namespace Galvao.Application.UseCases.Showroom.CommandHandlers;

public class AddShowroomItemPhotoCommandHandler : ICommandHandler<AddShowroomItemPhotoCommand, Guid>
{
    private readonly IShowroomItemRepository _showroomItemRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AddShowroomItemPhotoCommandHandler(
        IShowroomItemRepository showroomItemRepository,
        IUnitOfWork unitOfWork)
    {
        _showroomItemRepository = showroomItemRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> HandleAsync(AddShowroomItemPhotoCommand command, CancellationToken cancellationToken = default)
    {
        var item = await _showroomItemRepository.GetByIdAsync(command.ShowroomItemId, cancellationToken);
        if (item is null)
            return Result.Failure<Guid>(new Error("ShowroomItem.NotFound", $"Showroom item with ID '{command.ShowroomItemId}' was not found."));

        var addPhotoResult = item.AddPhoto(command.Url, command.Caption, command.IsPrimary);
        if (addPhotoResult.IsFailure)
            return Result.Failure<Guid>(addPhotoResult.Error);

        await _showroomItemRepository.AddPhotoAsync(addPhotoResult.Value, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return addPhotoResult.Value.Id;
    }
}
