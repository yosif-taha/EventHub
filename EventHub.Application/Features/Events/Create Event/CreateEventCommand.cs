using EventHub.Application.Common.Responses;
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
    int MaxAttendees) : IRequest<RequestResult<Guid>>;
}
