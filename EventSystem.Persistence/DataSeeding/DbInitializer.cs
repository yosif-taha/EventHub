using EventHub.Application.Contracts;
using EventHub.Domin.Enums;
using EventHub.Domin.Models;
using EventHub.Persistence.Data.Contexts;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventHub.Persistence.DataSeeding
{
    public class DbInitializer(EventDbContext context ,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager) : IDbInitializer
    {
        private readonly EventDbContext _context = context;
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager = roleManager;

        public async Task IntiliazeAsync()
        {
            if (_context.Database.GetPendingMigrationsAsync().GetAwaiter().GetResult().Any()) // GetPendingMigrationsAsync():- this fun to get all migration not appling to database. Any():- return true or false. 
            {
                await _context.Database.MigrateAsync();        //MigrateAsync() :- for appling migration in DB.
            }


            if(!_context.Roles.Any())
            {
               await  _roleManager.CreateAsync(new IdentityRole<Guid> () { Name = UserRole.Admin.ToString() });
               await  _roleManager.CreateAsync(new IdentityRole<Guid> () { Name = UserRole.Organizer.ToString() });
               await  _roleManager.CreateAsync(new IdentityRole<Guid> () { Name = UserRole.Attend.ToString() });
            }

            if(!_context.Users.Any())
            {
                var user1 = new ApplicationUser()
                {
                    Email = "AhmedSamy@gmail.com",
                    UserName = "AhmedSamy@gmail.com",
                    FullName = "Ahmed Samy",
                    PhoneNumber = "1239877890",
                };
                var user2 = new ApplicationUser()
                {
                    Email = "MohamedHany@gmail.com",
                    UserName = "MohamedHany@gmail.com",
                    FullName = "Mohamed Hany",
                    PhoneNumber = "4598598000",
                };
                var user3 = new ApplicationUser()
                {
                    Email = "SaraHossam@gmail.com",
                    UserName = "SaraHossam@gmail.com",
                    FullName = "Sara ossam",
                    PhoneNumber = "9847200422",
                };

                await _userManager.CreateAsync(user1,"P@ssword123");
                await _userManager.CreateAsync(user2,"P@ssword456");
                await _userManager.CreateAsync(user3,"P@ssword789");

                await _userManager.AddToRoleAsync(user1,UserRole.Admin.ToString());
                await _userManager.AddToRoleAsync(user2,UserRole.Attend.ToString());
                await _userManager.AddToRoleAsync(user3,UserRole.Organizer.ToString());
            }
        }
    }
}
