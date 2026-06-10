using Galvao.Application.Common.CQRS;
using Galvao.Application.Common.Interfaces;
using Galvao.Application.UseCases.Showroom.Commands;
using Galvao.Shared;

namespace Galvao.Application.UseCases.Showroom.CommandHandlers;

public class UpdateShowroomItemCommandHandler : ICommandHandler<UpdateShowroomItemCommand>
{
    private readonly IShowroomItemRepository _showroomItemRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateShowroomItemCommandHandler(
        IShowroomItemRepository showroomItemRepository,
        IUnitOfWork unitOfWork)
    {
        _showroomItemRepository = showroomItemRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> HandleAsync(UpdateShowroomItemCommand command, CancellationToken cancellationToken = default)
    {
        var item = await _showroomItemRepository.GetByIdAsync(command.Id, cancellationToken);
        if (item is null)
            return Result.Failure(new Error("ShowroomItem.NotFound", $"Showroom item with ID '{command.Id}' was not found."));

        var updateResult = item.UpdateDetails(command.Title, command.Description, command.Price, command.Category);
        if (updateResult.IsFailure)
            return updateResult;

        _showroomItemRepository.Update(item);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
