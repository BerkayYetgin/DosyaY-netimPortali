using dosyayonetim.Models;
using Microsoft.AspNetCore.Identity;

namespace dosyayonetim.Extensions
{
    public static class DbSeeder
    {
        public static async Task SeedRolesAndAdminAsync(IServiceProvider service)
        {
            //Seed Roles
            var userManager = service.GetService<UserManager<ApplicationUser>>();
            var roleManager = service.GetService<RoleManager<IdentityRole>>();
            var dbContext = service.GetService<AppDbContext>();

            await roleManager.CreateAsync(new IdentityRole(Roles.Admin));
            await roleManager.CreateAsync(new IdentityRole(Roles.User));

            // Seed Default Admin
            var admin = new ApplicationUser
            {
                UserName = "mehmet@example.com",
                Email = "admin@example.com",
                FirstName = "System",
                LastName = "Admin",
                EmailConfirmed = true
            };

            if (await userManager.FindByEmailAsync(admin.Email) == null)
            {
                await userManager.CreateAsync(admin, "MehmetAdmin@123");
                await userManager.AddToRoleAsync(admin, Roles.Admin);

                // Create admin storage with unlimited space
                var adminStorage = new UserStorage
                {
                    UserId = admin.Id,
                    TotalStorage = long.MaxValue, // Unlimited storage
                    UsedStorage = 0,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                dbContext.UserStorages.Add(adminStorage);
                await dbContext.SaveChangesAsync();
            }
        }
    }
}