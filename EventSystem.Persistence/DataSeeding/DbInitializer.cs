using EventHub.Application.Contracts;
using EventHub.Domin.Enums;
using EventHub.Domin.Models;
using EventHub.Persistence.Data.Contexts;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EventHub.Persistence.DataSeeding
{
    public class DbInitializer(EventDbContext _context ,
        UserManager<ApplicationUser> _userManager,
        RoleManager<IdentityRole<Guid>> _roleManager) : IDbInitializer
    {
        public async Task IntiliazeAsync()
        {
            if (_context.Database.GetPendingMigrationsAsync().GetAwaiter().GetResult().Any()) // GetPendingMigrationsAsync():- this fun to get all migration not appling to database. Any():- return true or false. 
            {
                await _context.Database.MigrateAsync();        //MigrateAsync() :- for appling migration in DB.
            }


            await EnsureRoleExistsAsync(EventHub.Domin.Constants.RoleNames.Admin);
            await EnsureRoleExistsAsync(EventHub.Domin.Constants.RoleNames.Organizer);
            await EnsureRoleExistsAsync(EventHub.Domin.Constants.RoleNames.Attendee);
            await MigrateLegacyAttendeeRoleAsync();

            if(!_context.Users.Any())
            {
                var user1 = new ApplicationUser()
                {
                    Email = "AhmedSamy@gmail.com",
                    UserName = "AhmedSamy@gmail.com",
                    FullName = "Ahmed Samy",
                    PhoneNumber = "1239877890",
                    EmailConfirmed = true,
                    Role = UserRole.Admin,
                };
                var user2 = new ApplicationUser()
                {
                    Email = "MohamedHany@gmail.com",
                    UserName = "MohamedHany@gmail.com",
                    FullName = "Mohamed Hany",
                    PhoneNumber = "4598598000",
                    EmailConfirmed = true,
                    Role = UserRole.Attendee,
                };
                var user3 = new ApplicationUser()
                {
                    Email = "SaraHossam@gmail.com",
                    UserName = "SaraHossam@gmail.com",
                    FullName = "Sara ossam",
                    PhoneNumber = "9847200422",
                    EmailConfirmed = true,
                    Role = UserRole.Organizer,
                };

                await _userManager.CreateAsync(user1,"P@ssword123");
                await _userManager.CreateAsync(user2,"P@ssword456");
                await _userManager.CreateAsync(user3,"P@ssword789");

                await _userManager.AddToRoleAsync(user1, EventHub.Domin.Constants.RoleNames.Admin);
                await _userManager.AddToRoleAsync(user2, EventHub.Domin.Constants.RoleNames.Attendee);
                await _userManager.AddToRoleAsync(user3, EventHub.Domin.Constants.RoleNames.Organizer);
            }

            await SynchronizeApplicationRolesAsync();
        }

        private async Task EnsureRoleExistsAsync(string roleName)
        {
            if (await _roleManager.RoleExistsAsync(roleName))
                return;

            var result = await _roleManager.CreateAsync(new IdentityRole<Guid> { Name = roleName });
            if (!result.Succeeded)
                throw new InvalidOperationException($"Unable to create the '{roleName}' role.");
        }

        private async Task MigrateLegacyAttendeeRoleAsync()
        {
            const string legacyRoleName = "Attend";
            var legacyRole = await _roleManager.FindByNameAsync(legacyRoleName);
            if (legacyRole is null)
                return;

            var users = await _userManager.GetUsersInRoleAsync(legacyRoleName);
            foreach (var user in users)
            {
                if (!await _userManager.IsInRoleAsync(user, EventHub.Domin.Constants.RoleNames.Attendee))
                    await _userManager.AddToRoleAsync(user, EventHub.Domin.Constants.RoleNames.Attendee);

                await _userManager.RemoveFromRoleAsync(user, legacyRoleName);
            }

            await _roleManager.DeleteAsync(legacyRole);
        }

        private async Task SynchronizeApplicationRolesAsync()
        {
            var users = await _userManager.Users.ToListAsync();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                if (!roles.Any(role => role is EventHub.Domin.Constants.RoleNames.Admin or EventHub.Domin.Constants.RoleNames.Organizer or EventHub.Domin.Constants.RoleNames.Attendee))
                {
                    var roleResult = await _userManager.AddToRoleAsync(user, EventHub.Domin.Constants.RoleNames.Attendee);
                    if (!roleResult.Succeeded)
                        throw new InvalidOperationException($"Unable to assign the attendee role to user '{user.Id}'.");

                    roles = await _userManager.GetRolesAsync(user);
                }

                var primaryRole = roles.Contains(EventHub.Domin.Constants.RoleNames.Admin)
                    ? UserRole.Admin
                    : roles.Contains(EventHub.Domin.Constants.RoleNames.Organizer)
                        ? UserRole.Organizer
                        : UserRole.Attendee;

                var isLegacySeedUser = user.Email is "AhmedSamy@gmail.com" or "MohamedHany@gmail.com" or "SaraHossam@gmail.com";
                if (user.Role == primaryRole && (!isLegacySeedUser || user.EmailConfirmed))
                    continue;

                user.Role = primaryRole;
                if (isLegacySeedUser)
                    user.EmailConfirmed = true;
                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                    throw new InvalidOperationException($"Unable to synchronize role for user '{user.Id}'.");
            }
        }
    }
}
