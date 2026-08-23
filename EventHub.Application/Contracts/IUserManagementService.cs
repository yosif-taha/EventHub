using EventHub.Application.Common.Dtos.Admin;
using EventHub.Application.Common.Responses;
using EventHub.Domin.Enums;

namespace EventHub.Application.Contracts
{
    public interface IUserManagementService
    {
        Task<RequestResult<IReadOnlyList<UserRoleDto>>> GetUsersAsync(CancellationToken ct = default);
        Task<RequestResult<bool>> UpdateUserRoleAsync(Guid userId, UserRole role, CancellationToken ct = default);
    }
}
