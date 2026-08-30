using EventHub.Application.Common.Dtos.Admin;
using EventHub.Application.Common.Models;
using EventHub.Application.Common.Responses;
using EventHub.Domin.Enums;

namespace EventHub.Application.Contracts
{
    public interface IUserManagementService
    {
        Task<RequestResult<PaginatedList<UserRoleDto>>> GetUsersAsync(int pageNumber, int pageSize, string? searchValue, CancellationToken ct = default);
        Task<int> GetUserCountAsync(CancellationToken ct = default);
        Task<RequestResult<bool>> UpdateUserRoleAsync(Guid userId, UserRole role, CancellationToken ct = default);
    }
}
