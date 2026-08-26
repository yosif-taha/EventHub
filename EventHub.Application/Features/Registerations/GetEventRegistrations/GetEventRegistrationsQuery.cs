using EventHub.Application.Common.Dtos.Registrations;
using EventHub.Application.Common.Models;
using EventHub.Application.Common.Responses;
using MediatR;

namespace EventHub.Application.Features.Registerations.GetEventRegistrations
{
    public record GetEventRegistrationsQuery(Guid EventId, int PageNumber = 1, int PageSize = 10)
        : IRequest<RequestResult<PaginatedList<EventRegistrationDto>>>;
}
