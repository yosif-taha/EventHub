using EventHub.Domin.Enums;
using EventHub.MVC.Models.Admins;
using EventHub.MVC.Models.Common;
using EventHub.MVC.Models.Events;
using EventHub.MVC.Models.Organizers;
using EventHub.MVC.Services.Api;

namespace EventHub.MVC.Services.Admins;

public interface IAdminApiClient
{
    Task<ApiCallResult<AdminDashboardDto>> GetDashboardAsync(CancellationToken cancellationToken);
    Task<ApiCallResult<PaginatedResult<AdminUserDto>>> GetUsersAsync(AdminUserListFilter filter, CancellationToken cancellationToken);
    Task<ApiCallResult<object>> UpdateUserRoleAsync(Guid userId, UserRole role, CancellationToken cancellationToken);
    Task<ApiCallResult<PaginatedResult<EventSummaryDto>>> GetEventsAsync(EventListFilter filter, CancellationToken cancellationToken);
    Task<ApiCallResult<EventDetailsDto>> GetEventAsync(Guid eventId, CancellationToken cancellationToken);
    Task<ApiCallResult<Guid>> CreateEventAsync(CreateOrganizerEventRequest request, CancellationToken cancellationToken);
    Task<ApiCallResult<object>> UpdateEventAsync(Guid eventId, UpdateOrganizerEventRequest request, CancellationToken cancellationToken);
    Task<ApiCallResult<object>> UpdateEventStatusAsync(Guid eventId, EventStatus status, CancellationToken cancellationToken);
    Task<ApiCallResult<bool>> DeleteEventAsync(Guid eventId, CancellationToken cancellationToken);
    Task<ApiCallResult<PaginatedResult<AdminEventRegistrationDto>>> GetEventRegistrationsAsync(Guid eventId, int pageNumber, int pageSize, CancellationToken cancellationToken);
    Task<ApiCallResult<PaginatedResult<AdminRegistrationDto>>> GetRegistrationsAsync(int pageNumber, int pageSize, CancellationToken cancellationToken);
    Task<ApiCallResult<object>> SendAnnouncementAsync(SendEventAnnouncementRequest request, CancellationToken cancellationToken);
}
