using EventHub.Application.Common.Dtos.Dashboards;
using EventHub.Application.Common.Responses;
using MediatR;

namespace EventHub.Application.Features.Dashboards.GetOrganizerDashboard
{
    public record GetOrganizerDashboardQuery : IRequest<RequestResult<OrganizerDashboardDto>>;
}
