using EventHub.Application.Common.Dtos.Events;
using EventHub.Application.Common.Models;
using EventHub.Application.Common.Responses;
using MediatR;

namespace EventHub.Application.Features.Events.GetAll_Events
{
    public record GetAllEventsQuery(
       string? SearchValue,
       Guid? CategoryId,
       string? SortColumn,
       string? SortDirection = "asc",
       int PageNumber = 1,
       int PageSize = 10) : IRequest<RequestResult<PaginatedList<EventDto>>>;
   
}
