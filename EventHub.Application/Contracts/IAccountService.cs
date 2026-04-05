using EventHub.Application.Common.Dtos.Account;
using EventHub.Application.Common.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventHub.Application.Contracts
{
    public interface IAccountService
    {
        Task<RequestResult<UserProfileResponse>> GetUserProfileAsync(string userId , CancellationToken ct = default);
        Task<RequestResult<bool>> UpdateUserProfileAsync(string userId, string fullName , CancellationToken ct = default);
        Task<RequestResult<bool>> ChangePasswordAsync(string userId, string currentPassword, string newPassword, CancellationToken ct = default);
    }
}
