using EventHub.Application.Common.Dtos.Account;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventHub.Application.Contracts
{
    public interface IAccountService
    {
        Task<UserProfileResponse> GetUserProfileAsync(string userId);
        Task<bool> UpdateUserProfileAsync(string userId, string fullName);
        Task<bool> ChangePasswordAsync(string userId, string currentPassword, string newPassword);
    }
}
