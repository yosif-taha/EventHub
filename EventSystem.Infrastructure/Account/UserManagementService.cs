using EventHub.Application.Common.Dtos.Admin;
using EventHub.Application.Common.Models;
using EventHub.Application.Common.Responses;
using EventHub.Application.Contracts;
using EventHub.Domin.Constants;
using EventHub.Domin.Enums;
using EventHub.Domin.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace EventHub.Infrastructure.Account
{
    public class UserManagementService(
        UserManager<ApplicationUser> _userManager,
        RoleManager<IdentityRole<Guid>> _roleManager,
        IUnitOfWork _unitOfWork) : IUserManagementService
    {
        public Task<int> GetUserCountAsync(CancellationToken ct) => _userManager.Users.CountAsync(ct);

        public async Task<RequestResult<PaginatedList<UserRoleDto>>> GetUsersAsync(
            int pageNumber,
            int pageSize,
            string? searchValue,
            CancellationToken ct)
        {
            var normalizedSearch = searchValue?.Trim();
            var query = _userManager.Users.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(normalizedSearch))
            {
                query = query.Where(user =>
                    (user.Email != null && user.Email.Contains(normalizedSearch)) ||
                    user.FullName.Contains(normalizedSearch));
            }

            var totalCount = await query.CountAsync(ct);
            var users = await query
                .OrderBy(user => user.Email)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(user => new { user.Id, user.Email, user.FullName, user.Role })
                .ToListAsync(ct);

            var result = users
                .Select(user => new UserRoleDto(user.Id, user.Email ?? string.Empty, user.FullName, user.Role.ToString()))
                .ToList();

            return RequestResult<PaginatedList<UserRoleDto>>.Success(
                new PaginatedList<UserRoleDto>(result, totalCount, pageNumber, pageSize));
        }

        public async Task<RequestResult<bool>> UpdateUserRoleAsync(Guid userId, UserRole role, CancellationToken ct)
        {
            return await _unitOfWork.ExecuteAsync(async () =>
            {
                var user = await _userManager.FindByIdAsync(userId.ToString());
                if (user is null)
                    return RequestResult<bool>.Failure(ErrorCode.UserNotFound);

                var requestedRoleName = GetRoleName(role);
                if (!await _roleManager.RoleExistsAsync(requestedRoleName))
                    return RequestResult<bool>.Failure(ErrorCode.RoleNotFound);

                var currentRoles = await _userManager.GetRolesAsync(user);
                if (currentRoles.Contains(RoleNames.Admin) && role != UserRole.Admin)
                {
                    var administrators = await _userManager.GetUsersInRoleAsync(RoleNames.Admin);
                    if (administrators.Count <= 1)
                        return RequestResult<bool>.Failure(ErrorCode.ValidationError, "The final remaining Administrator cannot be demoted.");
                }

                if (!currentRoles.Contains(requestedRoleName))
                {
                    var addResult = await _userManager.AddToRoleAsync(user, requestedRoleName);
                    if (!addResult.Succeeded)
                        return RequestResult<bool>.Failure(ErrorCode.DatabaseError);
                }

                var rolesToRemove = currentRoles.Where(currentRole => currentRole != requestedRoleName).ToArray();
                if (rolesToRemove.Length > 0)
                {
                    var removeResult = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
                    if (!removeResult.Succeeded)
                        return RequestResult<bool>.Failure(ErrorCode.DatabaseError);
                }

                user.Role = role;
                var updateResult = await _userManager.UpdateAsync(user);
                return updateResult.Succeeded
                    ? RequestResult<bool>.Success(true)
                    : RequestResult<bool>.Failure(ErrorCode.DatabaseError);
            }, IsolationLevel.Serializable, ct);
        }

        private static string GetRoleName(UserRole role) => role switch
        {
            UserRole.Admin => RoleNames.Admin,
            UserRole.Organizer => RoleNames.Organizer,
            _ => RoleNames.Attendee
        };

    }
}
