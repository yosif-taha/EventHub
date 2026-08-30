using EventHub.Application.Contracts;
using EventHub.Domin.Enums;
using EventHub.Domin.Models;
using EventHub.Persistence.Data.Contexts;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.ComponentModel.DataAnnotations;

namespace EventHub.Persistence.DataSeeding
{
    public class DbInitializer(EventDbContext _context ,
        UserManager<ApplicationUser> _userManager,
        RoleManager<IdentityRole<Guid>> _roleManager,
        IConfiguration configuration) : IDbInitializer
    {
        private readonly BootstrapAdminSettings _bootstrapAdminSettings = new()
        {
            Enabled = bool.TryParse(configuration["BootstrapAdminSettings:Enabled"], out var enabled) && enabled,
            Email = configuration["BootstrapAdminSettings:Email"],
            Password = configuration["BootstrapAdminSettings:Password"],
            FullName = configuration["BootstrapAdminSettings:FullName"]
        };
        private readonly bool _isDevelopment = string.Equals(
            configuration["ASPNETCORE_ENVIRONMENT"] ?? configuration["DOTNET_ENVIRONMENT"],
            "Development",
            StringComparison.OrdinalIgnoreCase);

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

            if (!_isDevelopment)
                await BootstrapAdminAsync();

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

                if (user.Role == primaryRole)
                    continue;

                user.Role = primaryRole;
                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                    throw new InvalidOperationException($"Unable to synchronize role for user '{user.Id}'.");
            }
        }

        private async Task BootstrapAdminAsync()
        {
            if (!_bootstrapAdminSettings.Enabled)
                return;

            var validationResults = new List<ValidationResult>();
            if (!Validator.TryValidateObject(_bootstrapAdminSettings, new ValidationContext(_bootstrapAdminSettings), validationResults, true))
                throw new InvalidOperationException("Bootstrap administrator configuration is invalid.");

            var existingAdministrators = await _userManager.GetUsersInRoleAsync(EventHub.Domin.Constants.RoleNames.Admin);
            if (existingAdministrators.Count > 0)
                return;

            var existingUser = await _userManager.FindByEmailAsync(_bootstrapAdminSettings.Email!);
            if (existingUser is not null)
                throw new InvalidOperationException("Bootstrap administrator email already belongs to a user. Assign the administrator role explicitly.");

            var administrator = new ApplicationUser
            {
                Email = _bootstrapAdminSettings.Email,
                UserName = _bootstrapAdminSettings.Email,
                FullName = _bootstrapAdminSettings.FullName!,
                EmailConfirmed = true,
                Role = UserRole.Admin
            };

            var createResult = await _userManager.CreateAsync(administrator, _bootstrapAdminSettings.Password!);
            if (!createResult.Succeeded)
                throw new InvalidOperationException("Unable to create the configured bootstrap administrator.");

            var roleResult = await _userManager.AddToRoleAsync(administrator, EventHub.Domin.Constants.RoleNames.Admin);
            if (roleResult.Succeeded)
                return;

            await _userManager.DeleteAsync(administrator);
            throw new InvalidOperationException("Unable to assign the administrator role to the configured bootstrap administrator.");
        }
    }
}
