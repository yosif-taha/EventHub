using EventHub.Application.Common.Responses;
using MediatR;

namespace EventHub.Application.Features.Events.Update_Event
{
    public record UpdateEventCommand(
      Guid Id,
      string? Title,
      string? Description,
      DateTime? EventDate,
      string? Location,
      Guid? CategoryId,
      int? MaxAttendees) : IRequest<RequestResult<Unit>>;
}
