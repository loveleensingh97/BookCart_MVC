using BookCart.DataAccess.Data;
using BookCart.Models;
using BookCart.Utility;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookCart.DataAccess.DbInitializer
{
    public class DbInitializer : IDbInitializer
    {
        private readonly UserManager<IdentityUser> userManager;
        private readonly RoleManager<IdentityRole> roleManager;
        private readonly ApplicationDbContext dbContext;

        public DbInitializer(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager, ApplicationDbContext dbContext)
        {
            this.userManager = userManager;
            this.roleManager = roleManager;
            this.dbContext = dbContext;
        }

        public void Initialize()
        {
            //migrations if they are not applied
            try
            {
                if (dbContext.Database.GetPendingMigrations().Count() > 0)
                {
                    dbContext.Database.Migrate();
                }
            }
            catch (Exception ex) { }

            //create roles if they are not created
            if (!roleManager.RoleExistsAsync(StaticDetails.Role_Customer).GetAwaiter().GetResult())
            {
                roleManager.CreateAsync(new IdentityRole(StaticDetails.Role_Customer)).GetAwaiter().GetResult();
                roleManager.CreateAsync(new IdentityRole(StaticDetails.Role_Employee)).GetAwaiter().GetResult();
                roleManager.CreateAsync(new IdentityRole(StaticDetails.Role_Admin)).GetAwaiter().GetResult();
                roleManager.CreateAsync(new IdentityRole(StaticDetails.Role_Company)).GetAwaiter().GetResult();

                //if roles are not created, then we will create admin user as well
                userManager.CreateAsync(new ApplicationUser
                {
                    UserName = "loveleensingh1997@gmail.com",
                    Email = "loveleensingh1997@gmail.com",
                    Name = "Loveleen Singh",
                    PhoneNumber = "7988578813",
                    StreetAddress = "Sector 88A/89A",
                    State = "Haryana",
                    PostalCode = "122505",
                    City = "Gurgaon"
                }, "Abcd1234@").GetAwaiter().GetResult();

                ApplicationUser user = dbContext.ApplicationUsers.FirstOrDefault(u => u.Email == "loveleensingh1997@gmail.com");
                userManager.AddToRoleAsync(user, StaticDetails.Role_Admin).GetAwaiter().GetResult();
            }
            return;           
        }
    }
}
