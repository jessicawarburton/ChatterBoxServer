using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Server.Data.Models;

namespace Server.Data;

public class DbInitializer(
    RoleManager<IdentityRole> roleManager,
    UserManager<ApplicationUser> userManager)
{
    public async Task SeedAsync()
    {
        // Create administrator role 
        var administratorRole = new IdentityRole("Administrator");
        if (roleManager.Roles.All(r => r.Name != administratorRole.Name))
        {
            var role = await roleManager.CreateAsync(administratorRole);
            await roleManager.AddClaimAsync(administratorRole, new Claim("RoleClaim", "HasRoleView"));
            await roleManager.AddClaimAsync(administratorRole, new Claim("RoleClaim", "HasRoleAdd"));
            await roleManager.AddClaimAsync(administratorRole, new Claim("RoleClaim", "HasRoleEdit"));
            await roleManager.AddClaimAsync(administratorRole, new Claim("RoleClaim", "HasRoleDelete"));
        }
        
        // Create user role 
        var userRole = new IdentityRole("User");
        if (roleManager.Roles.All(r => r.Name != userRole.Name))
        {
            var role = await roleManager.CreateAsync(userRole);
            await roleManager.AddClaimAsync(userRole, new Claim("RoleClaim", "HasRoleView"));
        }

        // Create default administrator 
        var administrator = new ApplicationUser { UserName = "Administrator", Email = "administrator@example.com" };
        if (userManager.Users.All(u => u.UserName != administrator.UserName))
        {
            await userManager.CreateAsync(administrator, "Administrator123!");
            if (administratorRole.Name != null)
                await userManager.AddToRoleAsync(administrator, administratorRole.Name);
        }
    }
    
}