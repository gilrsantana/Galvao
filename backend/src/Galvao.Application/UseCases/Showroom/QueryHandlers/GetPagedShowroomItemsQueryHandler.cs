using Galvao.Application.Common.CQRS;
using Galvao.Application.Common.Interfaces;
using Galvao.Application.UseCases.Showroom.Queries;
using Galvao.Shared;

namespace Galvao.Application.UseCases.Showroom.QueryHandlers;

public class GetPagedShowroomItemsQueryHandler : IQueryHandler<GetPagedShowroomItemsQuery, PagedResponse<ShowroomItemResponse>>
{
    private readonly IShowroomItemRepository _showroomItemRepository;

    public GetPagedShowroomItemsQueryHandler(IShowroomItemRepository showroomItemRepository)
    {
        _showroomItemRepository = showroomItemRepository;
    }

    public async Task<Result<PagedResponse<ShowroomItemResponse>>> HandleAsync(GetPagedShowroomItemsQuery query, CancellationToken cancellationToken = default)
    {
        var pagedItems = await _showroomItemRepository.GetPagedAsync(query.Page, query.PageSize, cancellationToken);

        var mappedItems = pagedItems.Items.Select(item => new ShowroomItemResponse(
            item.Id,
            item.Title,
            item.Description,
            item.Price,
            item.Category,
            item.Active,
            item.CreatedAt,
            item.UpdatedAt,
            item.Photos.Select(p => new ShowroomItemPhotoResponse(p.Id, p.Url, p.Caption, p.IsPrimary)).ToList()
        )).ToList();

        var response = new PagedResponse<ShowroomItemResponse>(
            mappedItems,
            pagedItems.TotalCount,
            pagedItems.PageNumber,
            pagedItems.PageSize);

        return response;
    }
}
