using Galvao.Application.Common.CQRS;
using Galvao.Application.Common.Interfaces;
using Galvao.Application.Common.Models;
using Galvao.Application.UseCases.Showroom.Queries;
using Galvao.Domain.ShowroomAggregate.Entities;
using Galvao.Shared;

namespace Galvao.Application.UseCases.Showroom.QueryHandlers;

public class GetPagedShowroomItemsQueryHandler(IShowroomItemRepository showroomItemRepository) 
    : IQueryHandler<GetPagedShowroomItemsQuery, PagedResponse<ShowroomItemResponse>>
{
    public async Task<Result<PagedResponse<ShowroomItemResponse>>> HandleAsync(GetPagedShowroomItemsQuery query, CancellationToken cancellationToken = default)
    {
        var advancedQuery = new AdvancedQuery<ShowroomItem>
        {
            Skip = (query.Page - 1) * query.PageSize,
            Take = query.PageSize,
            NoTracking = true,
            Includes = [x => x.Photos],
            Ordering =
            [
                new OrderingItem
                {
                    Field = "CreatedAt",
                    Direction = SortingDirection.Descending
                }
            ]
        };

        var pagedItems = await showroomItemRepository.AdvancedQueryAsync(advancedQuery, cancellationToken);

        List<ShowroomItemResponse> mappedItems =
        [.. pagedItems.Items.Select(item => new ShowroomItemResponse(
            item.Id,
            item.Title,
            item.Description,
            item.Price,
            item.Category,
            item.Active,
            item.CreatedAt,
            item.UpdatedAt,
            [.. item.Photos.Select(p => new ShowroomItemPhotoResponse(
                p.Id,
                p.Url,
                p.Caption,
                p.IsPrimary))]
        ))];

        var response = new PagedResponse<ShowroomItemResponse>(
            mappedItems,
            pagedItems.TotalCount,
            pagedItems.PageNumber,
            pagedItems.PageSize);

        return response;
    }
}
