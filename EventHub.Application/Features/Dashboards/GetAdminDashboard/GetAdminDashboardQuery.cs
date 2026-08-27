using EventHub.Application.Common.Dtos.Dashboards;
using EventHub.Application.Common.Responses;
using MediatR;

namespace EventHub.Application.Features.Dashboards.GetAdminDashboard
{
    public record GetAdminDashboardQuery : IRequest<RequestResult<AdminDashboardDto>>;
}
