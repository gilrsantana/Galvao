using Galvao.Application.Common.CQRS;
using Galvao.Application.Common.Interfaces;
using Galvao.Application.UseCases.Showroom.Commands;
using Galvao.Domain.ShowroomAggregate.Entities;
using Galvao.Shared;

namespace Galvao.Application.UseCases.Showroom.CommandHandlers;

public class CreateShowroomItemCommandHandler : ICommandHandler<CreateShowroomItemCommand, Guid>
{
    private readonly IShowroomItemRepository _showroomItemRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateShowroomItemCommandHandler(
        IShowroomItemRepository showroomItemRepository,
        IUnitOfWork unitOfWork)
    {
        _showroomItemRepository = showroomItemRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> HandleAsync(CreateShowroomItemCommand command, CancellationToken cancellationToken = default)
    {
        var itemResult = ShowroomItem.Create(command.Title, command.Description, command.Price, command.Category);
        if (itemResult.IsFailure)
            return Result.Failure<Guid>(itemResult.Error);

        var item = itemResult.Value;

        await _showroomItemRepository.AddAsync(item, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return item.Id;
    }
}
