using Microsoft.AspNetCore.Identity;
using QuoteManager.Constants;
using QuoteManager.Models;

namespace QuoteManager.Services
{
    public class DbSeeder
    {
        public static async Task SeedRolesAndSuperAdmin(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            // Step 1: Create the 4 roles
            string[] roleNames = { Roles.SuperAdmin, Roles.Admin, Roles.Staff, Roles.Client };

            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // Step 2: Create SuperAdmin user
            var superAdminEmail = "superadmin@quotemanager.com";
            var superAdminUser = await userManager.FindByEmailAsync(superAdminEmail);

            if (superAdminUser == null)
            {
                var newSuperAdmin = new ApplicationUser
                {
                    UserName = superAdminEmail,
                    Email = superAdminEmail,
                    FullName = "Super Administrator",
                    EmailConfirmed = true,
                    CreatedAt = DateTime.UtcNow
                };

                var result = await userManager.CreateAsync(newSuperAdmin, "Super@123");

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(newSuperAdmin, Roles.SuperAdmin);
                }
            }
            else
            {
                // Ensure SuperAdmin has the SuperAdmin role
                if (!await userManager.IsInRoleAsync(superAdminUser, Roles.SuperAdmin))
                {
                    await userManager.AddToRoleAsync(superAdminUser, Roles.SuperAdmin);
                }
            }
        }
    }
}