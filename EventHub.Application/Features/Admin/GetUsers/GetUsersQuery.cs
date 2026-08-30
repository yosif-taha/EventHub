using EventHub.Application.Common.Dtos.Admin;
using EventHub.Application.Common.Models;
using EventHub.Application.Common.Responses;
using MediatR;

namespace EventHub.Application.Features.Admin.GetUsers
{
    public record GetUsersQuery(int PageNumber, int PageSize, string? SearchValue) : IRequest<RequestResult<PaginatedList<UserRoleDto>>>;
}
