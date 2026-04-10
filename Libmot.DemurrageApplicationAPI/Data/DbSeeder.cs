using Libmot.DemurrageApplicationAPI.Models;
using Microsoft.AspNetCore.Identity;

namespace Libmot.DemurrageApplicationAPI.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

            string[] roles = ["SuperAdmin", "FinanceBillingOfficer", "Customer", "DriverDispatch"];

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            // Seed default SuperAdmin
            const string adminEmail = "admin@libmotexpress.com";
            if (await userManager.FindByEmailAsync(adminEmail) == null)
            {
                var admin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "System Administrator",
                    CompanyName = "Libmot Express",
                    IsActive = true,
                    EmailConfirmed = true
                };
                var result = await userManager.CreateAsync(admin, "Admin@12345");
                if (result.Succeeded)
                    await userManager.AddToRoleAsync(admin, "SuperAdmin");
            }

            // Seed default DemurrageTiers
            var db = services.GetRequiredService<AppDbContext>();
            if (!db.DemurrageTiers.Any())
            {
                db.DemurrageTiers.AddRange(
                    new DemurrageTier { TierName = "Tier 1", DayFrom = 1, DayTo = 7, RatePerDay = 15000 },
                    new DemurrageTier { TierName = "Tier 2", DayFrom = 8, DayTo = 14, RatePerDay = 25000 },
                    new DemurrageTier { TierName = "Tier 3", DayFrom = 15, DayTo = 9999, RatePerDay = 40000 }
                );
                await db.SaveChangesAsync();
            }
        }
    }
}
