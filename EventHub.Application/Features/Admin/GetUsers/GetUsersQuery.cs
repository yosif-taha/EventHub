using EventHub.Application.Common.Dtos.Admin;
using EventHub.Application.Common.Responses;
using MediatR;

namespace EventHub.Application.Features.Admin.GetUsers
{
    public record GetUsersQuery : IRequest<RequestResult<IReadOnlyList<UserRoleDto>>>;
}
