using Galvao.Application.Common.CQRS;
using Galvao.Application.Common.Interfaces;
using Galvao.Application.UseCases.Showroom.Commands;
using Galvao.Shared;

namespace Galvao.Application.UseCases.Showroom.CommandHandlers;

public class UpdateShowroomItemPhotoCommandHandler : ICommandHandler<UpdateShowroomItemPhotoCommand>
{
    private readonly IShowroomItemRepository _showroomItemRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateShowroomItemPhotoCommandHandler(
        IShowroomItemRepository showroomItemRepository,
        IUnitOfWork unitOfWork)
    {
        _showroomItemRepository = showroomItemRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> HandleAsync(UpdateShowroomItemPhotoCommand command, CancellationToken cancellationToken = default)
    {
        var item = await _showroomItemRepository.GetByIdAsync(command.ShowroomItemId, cancellationToken);
        if (item is null)
            return Result.Failure(new Error("ShowroomItem.NotFound", $"Showroom item with ID '{command.ShowroomItemId}' was not found."));

        var updateResult = item.UpdatePhoto(command.PhotoId, command.Url, command.Caption, command.IsPrimary);
        if (updateResult.IsFailure)
            return updateResult;

        _showroomItemRepository.Update(item);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
