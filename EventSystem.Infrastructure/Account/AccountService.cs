using AutoMapper.QueryableExtensions;
using EventHub.Application.Common.Dtos.Account;
using EventHub.Application.Common.Responses;
using EventHub.Application.Contracts;
using EventHub.Domin.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventHub.Infrastructure.Account
{
    public class AccountService(UserManager<ApplicationUser> userManager) : IAccountService
    {
        private readonly UserManager<ApplicationUser> _userManager = userManager;

        public async Task<RequestResult<UserProfileResponse>> GetUserProfileAsync(string userId, CancellationToken ct = default)
        {
            var user = await _userManager.Users
                .Where(u => u.Id.ToString() == userId)
                .SingleAsync();
            if (user == null)         
                return RequestResult<UserProfileResponse>.Failure(ErrorCode.UserNotFound);
            return RequestResult<UserProfileResponse>.Success(new UserProfileResponse(user.Email!, user.UserName!, user.FullName));
        }
        public async Task<RequestResult<bool>> UpdateUserProfileAsync(string userId, string fullName , CancellationToken ct = default)
        {
            var result = await _userManager.Users
                .Where(u => u.Id.ToString() == userId)
                .ExecuteUpdateAsync(setters  => 
                  setters.SetProperty(u => u.FullName, fullName)
                );
          if(result == 0)
                return RequestResult<bool>.Failure(ErrorCode.InternalServerError);
            return RequestResult<bool>.Success(true);
        }
        public async Task<RequestResult<bool>> ChangePasswordAsync(string userId, string currentPassword, string newPassword , CancellationToken ct = default) 
        {
            var user = await _userManager.FindByIdAsync(userId);
            var result = await _userManager.ChangePasswordAsync(user!, currentPassword, newPassword);
            if (!result.Succeeded)
                 return RequestResult<bool>.Failure(ErrorCode.InternalServerError);
            return RequestResult<bool>.Success(result.Succeeded);   
        }
    }
}
