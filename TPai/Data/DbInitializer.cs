using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TPai.Models;

namespace TPai.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<RentCarDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        // Apply pending migrations so the app also works on a brand new machine.
        await context.Database.MigrateAsync();

        string[] roles = ["ADMIN", "USER"];
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        const string adminEmail = "admin@ayoub.tn";
        const string adminPassword = "golf7";

        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser is null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(adminUser, adminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "ADMIN");
            }
        }
        else if (!await userManager.IsInRoleAsync(adminUser, "ADMIN"))
        {
            await userManager.AddToRoleAsync(adminUser, "ADMIN");
        }
    }
}
