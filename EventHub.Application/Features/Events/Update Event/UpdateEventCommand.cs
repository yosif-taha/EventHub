using EventHub.Application.Common.Responses;
using EventHub.Domin.Enums;
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
      int? MaxAttendees,
      EventMode? Mode,
      string? OnlineMeetingUrl) : IRequest<RequestResult<Unit>>;
}
