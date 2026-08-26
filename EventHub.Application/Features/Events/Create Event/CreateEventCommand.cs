using EventHub.Application.Common.Responses;
using EventHub.Domin.Enums;
using MediatR;

namespace EventHub.Application.Features.Events.Create_Event
{
    public record CreateEventCommand(
    string Title,
    string Description,
    DateTime EventDate,
    double Price,   
    string Location,
    Guid CategoryId,
    int MaxAttendees,
    EventMode Mode,
    string? OnlineMeetingUrl) : IRequest<RequestResult<Guid>>;
}
