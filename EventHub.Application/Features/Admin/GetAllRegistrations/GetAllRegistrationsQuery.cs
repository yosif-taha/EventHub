using EventHub.Application.Common.Dtos.Registrations;
using EventHub.Application.Common.Models;
using EventHub.Application.Common.Responses;
using MediatR;

namespace EventHub.Application.Features.Admin.GetAllRegistrations
{
    public record GetAllRegistrationsQuery(int PageNumber = 1, int PageSize = 10)
        : IRequest<RequestResult<PaginatedList<AdminRegistrationDto>>>;
}
