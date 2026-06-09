using Galvao.Application.Common.CQRS;
using Galvao.Application.Common.Interfaces;
using Galvao.Application.UseCases.Showroom.Queries;
using Galvao.Shared;

namespace Galvao.Application.UseCases.Showroom.QueryHandlers;

public class GetShowroomItemByIdQueryHandler : IQueryHandler<GetShowroomItemByIdQuery, ShowroomItemResponse>
{
    private readonly IShowroomItemRepository _showroomItemRepository;

    public GetShowroomItemByIdQueryHandler(IShowroomItemRepository showroomItemRepository)
    {
        _showroomItemRepository = showroomItemRepository;
    }

    public async Task<Result<ShowroomItemResponse>> HandleAsync(GetShowroomItemByIdQuery query, CancellationToken cancellationToken = default)
    {
        var item = await _showroomItemRepository.GetByIdAsync(query.ShowroomItemId, cancellationToken);
        if (item is null)
        {
            return Result.Failure<ShowroomItemResponse>(new Error("ShowroomItem.NotFound", $"Showroom item with ID '{query.ShowroomItemId}' was not found."));
        }

        var response = new ShowroomItemResponse(
            item.Id,
            item.Title,
            item.Description,
            item.Price,
            item.Category,
            item.Active,
            item.CreatedAt,
            item.UpdatedAt,
            item.Photos.Select(p => new ShowroomItemPhotoResponse(p.Id, p.Url, p.Caption, p.IsPrimary)).ToList());

        return response;
    }
}
