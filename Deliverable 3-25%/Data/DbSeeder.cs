using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProofPocket.Models;

namespace ProofPocket.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            var db = services.GetRequiredService<ApplicationDbContext>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var config = services.GetRequiredService<IConfiguration>();

            // Applies any pending EF Core migrations (creates the database if needed).
            // Create the first migration with:  Add-Migration InitialCreate
            await db.Database.MigrateAsync();

            // Roles
            foreach (var role in new[] { AppRoles.Admin, AppRoles.User })
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // Default admin account (change it in appsettings.json)
            var adminEmail = config["SeedAdmin:Email"] ?? "admin@proofpocket.com";
            var adminPassword = config["SeedAdmin:Password"] ?? "Admin@123";
            var adminName = config["SeedAdmin:FullName"] ?? "System Admin";

            if (await userManager.FindByEmailAsync(adminEmail) == null)
            {
                var admin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    FullName = adminName,
                    RegistrationDate = DateTime.Now
                };

                var result = await userManager.CreateAsync(admin, adminPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(admin, AppRoles.Admin);
                }
            }

            // A few starter categories so the product form is usable straight away.
            if (!await db.Categories.AnyAsync())
            {
                db.Categories.AddRange(
                    new Category { Name = "Electronics", Description = "Laptops, TVs, cameras and other electronic devices." },
                    new Category { Name = "Mobile Phones", Description = "Smartphones, tablets and accessories." },
                    new Category { Name = "Home Appliances", Description = "Refrigerators, washing machines, ACs and more." },
                    new Category { Name = "Furniture", Description = "Beds, sofas, tables and other furniture." }
                );
                await db.SaveChangesAsync();
            }
        }
    }
}
