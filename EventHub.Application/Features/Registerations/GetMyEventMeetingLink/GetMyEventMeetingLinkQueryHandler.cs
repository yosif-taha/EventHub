using EventHub.Application.Common.Dtos.Registrations;
using EventHub.Application.Common.Responses;
using EventHub.Application.Contracts;
using EventHub.Domin.Constants;
using EventHub.Domin.Enums;
using EventHub.Domin.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EventHub.Application.Features.Registerations.GetMyEventMeetingLink;

public sealed class GetMyEventMeetingLinkQueryHandler(
    IGenericRepository<Registration> registrationRepository,
    IUserContext userContext) : IRequestHandler<GetMyEventMeetingLinkQuery, RequestResult<EventMeetingLinkDto>>
{
    public async Task<RequestResult<EventMeetingLinkDto>> Handle(
        GetMyEventMeetingLinkQuery request,
        CancellationToken cancellationToken)
    {
        if (!userContext.IsInRole(RoleNames.Attendee))
            return RequestResult<EventMeetingLinkDto>.Failure(ErrorCode.Forbidden);

        var meetingLink = await registrationRepository.GetAll()
            .AsNoTracking()
            .Where(registration =>
                registration.EventId == request.EventId &&
                registration.UserId == userContext.UserId &&
                registration.Status == RegistrationStatus.Confirmed &&
                registration.Event.Mode == EventMode.Online &&
                registration.Event.OnlineMeetingUrl != null)
            .Select(registration => new EventMeetingLinkDto(
                registration.EventId,
                registration.Event.Title,
                registration.Event.EventDate,
                registration.Event.OnlineMeetingUrl!))
            .FirstOrDefaultAsync(cancellationToken);

        return meetingLink is null
            ? RequestResult<EventMeetingLinkDto>.Failure(ErrorCode.RegistrationNotFound)
            : RequestResult<EventMeetingLinkDto>.Success(meetingLink);
    }
}
