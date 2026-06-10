using Galvao.Application.Common.CQRS;

namespace Galvao.Application.UseCases.Showroom.Queries;

public record GetShowroomItemByIdQuery(Guid ShowroomItemId) : IQuery<ShowroomItemResponse>;
