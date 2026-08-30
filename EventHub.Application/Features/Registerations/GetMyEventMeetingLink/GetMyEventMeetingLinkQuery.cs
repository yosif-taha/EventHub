using EventHub.Application.Common.Dtos.Registrations;
using EventHub.Application.Common.Responses;
using MediatR;

namespace EventHub.Application.Features.Registerations.GetMyEventMeetingLink;

public sealed record GetMyEventMeetingLinkQuery(Guid EventId)
    : IRequest<RequestResult<EventMeetingLinkDto>>;
