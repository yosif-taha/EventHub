using AutoMapper.QueryableExtensions;
using EventHub.Application.Common.Dtos.Account;
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

        public async Task<UserProfileResponse> GetUserProfileAsync(string userId)
        {
            var user = await _userManager.Users
                .Where(u => u.Id.ToString() == userId)
                .SingleAsync();
            return new UserProfileResponse(user.Id.ToString(), user.UserName!, user.FullName);

        }
        public async Task<bool> UpdateUserProfileAsync(string userId, string fullName)
        {
            //var user = await _userManager.Users
            //     .Where(u => u.Id.ToString() == userId)
            //     .SingleAsync();

            //user.FullName = fullName;
            //var result = await _userManager.UpdateAsync(user);
            //return result.Succeeded;
            var result = await _userManager.Users
                .Where(u => u.Id.ToString() == userId)
                .ExecuteUpdateAsync(setters  => 
                  setters.SetProperty(u => u.FullName, fullName)
                );
            return result == 1 ? true : false;
        }
        public async Task<bool> ChangePasswordAsync(string userId, string currentPassword, string newPassword)
        {
            var user = await _userManager.FindByIdAsync(userId);
            var result = await _userManager.ChangePasswordAsync(user!, currentPassword, newPassword);
            return result.Succeeded;
        }
    }
}
