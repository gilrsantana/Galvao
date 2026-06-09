using Galvao.Application.Common.CQRS;
using Galvao.Shared;

namespace Galvao.Application.UseCases.Showroom.Queries;

public record GetPagedShowroomItemsQuery(int Page, int PageSize) : IQuery<PagedResponse<ShowroomItemResponse>>;
