using EventHub.Application.Common.Dtos.Dashboards;
using EventHub.Application.Common.Responses;
using EventHub.Application.Contracts;
using EventHub.Domin.Constants;
using EventHub.Domin.Models;
using MediatR;

namespace EventHub.Application.Features.Dashboards.GetOrganizerDashboard
{
    public class GetOrganizerDashboardQueryHandler(
        IGenericRepository<Event> _eventRepository,
        IGenericRepository<Registration> _registrationRepository,
        IUserContext _userContext,
        IDbExecutor _executor) : IRequestHandler<GetOrganizerDashboardQuery, RequestResult<OrganizerDashboardDto>>
    {
        public async Task<RequestResult<OrganizerDashboardDto>> Handle(GetOrganizerDashboardQuery request, CancellationToken cancellationToken)
        {
            if (!_userContext.IsInRole(RoleNames.Organizer))
                return RequestResult<OrganizerDashboardDto>.Failure(ErrorCode.Forbidden);

            var events = _eventRepository.GetAll().Where(@event => @event.OrganizerId == _userContext.UserId);
            var registrations = _registrationRepository.GetAll()
                .Where(registration => registration.Event.OrganizerId == _userContext.UserId);

            var eventStatusRows = await _executor.ToListAsync(
                events.GroupBy(@event => @event.Status)
                    .Select(group => new { Status = group.Key, Count = group.Count() }),
                cancellationToken);
            var totalEventRows = await _executor.ToListAsync(
                events.GroupBy(_ => 1).Select(group => group.Count()),
                cancellationToken);
            var totalRegistrationRows = await _executor.ToListAsync(
                registrations.GroupBy(_ => 1).Select(group => group.Count()),
                cancellationToken);
            var totalAttendeeRows = await _executor.ToListAsync(
                events.GroupBy(_ => 1).Select(group => group.Sum(@event => @event.CurrentAttendeesCount)),
                cancellationToken);

            return RequestResult<OrganizerDashboardDto>.Success(new OrganizerDashboardDto
            {
                TotalEvents = totalEventRows.FirstOrDefault(),
                TotalRegistrations = totalRegistrationRows.FirstOrDefault(),
                TotalAttendees = totalAttendeeRows.FirstOrDefault(),
                EventStatusOverview = eventStatusRows
                    .Select(row => new EventStatusCountDto(row.Status.ToString(), row.Count))
                    .ToList()
            });
        }
    }
}
