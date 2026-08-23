using EventHub.Application.Common.Responses;
using EventHub.Domin.Enums;
using MediatR;

namespace EventHub.Application.Features.Admin.UpdateUserRole
{
    public record UpdateUserRoleCommand(Guid UserId, UserRole Role) : IRequest<RequestResult<bool>>;
}
