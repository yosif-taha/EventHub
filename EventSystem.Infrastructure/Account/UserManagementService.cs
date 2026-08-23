using EventHub.Application.Common.Dtos.Admin;
using EventHub.Application.Common.Responses;
using EventHub.Application.Contracts;
using EventHub.Domin.Constants;
using EventHub.Domin.Enums;
using EventHub.Domin.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EventHub.Infrastructure.Account
{
    public class UserManagementService(
        UserManager<ApplicationUser> _userManager,
        RoleManager<IdentityRole<Guid>> _roleManager) : IUserManagementService
    {
        public async Task<RequestResult<IReadOnlyList<UserRoleDto>>> GetUsersAsync(CancellationToken ct)
        {
            var users = await _userManager.Users.OrderBy(user => user.Email).ToListAsync(ct);
            var result = new List<UserRoleDto>(users.Count);

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                result.Add(new UserRoleDto(user.Id, user.Email ?? string.Empty, user.FullName, GetPrimaryRole(roles).ToString()));
            }

            return RequestResult<IReadOnlyList<UserRoleDto>>.Success(result);
        }

        public async Task<RequestResult<bool>> UpdateUserRoleAsync(Guid userId, UserRole role, CancellationToken ct)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user is null)
                return RequestResult<bool>.Failure(ErrorCode.UserNotFound);

            var requestedRoleName = GetRoleName(role);
            if (!await _roleManager.RoleExistsAsync(requestedRoleName))
                return RequestResult<bool>.Failure(ErrorCode.RoleNotFound);

            var currentRoles = await _userManager.GetRolesAsync(user);
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
        }

        private static string GetRoleName(UserRole role) => role switch
        {
            UserRole.Admin => RoleNames.Admin,
            UserRole.Organizer => RoleNames.Organizer,
            _ => RoleNames.Attendee
        };

        private static UserRole GetPrimaryRole(IList<string> roles) => roles.Contains(RoleNames.Admin)
            ? UserRole.Admin
            : roles.Contains(RoleNames.Organizer)
                ? UserRole.Organizer
                : UserRole.Attendee;
    }
}
