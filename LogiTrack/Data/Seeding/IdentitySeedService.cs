using LogiTrack.Features.Auth;
using Microsoft.AspNetCore.Identity;

namespace LogiTrack.Data.Seeding;

public sealed class IdentitySeedService(
    RoleManager<IdentityRole> roleManager,
    UserManager<IdentityUser> userManager,
    IConfiguration configuration,
    ILogger<IdentitySeedService> logger)
{
    public async Task SeedIdentityAsync()
    {
        foreach (var role in new[] { AuthRoles.Operator, AuthRoles.Administrator })
        {
            if (await roleManager.RoleExistsAsync(role))
            {
                continue;
            }

            var result = await roleManager.CreateAsync(new IdentityRole(role));
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Could not create the {role} role: {string.Join(", ", result.Errors.Select(error => error.Code))}");
            }
        }

        var adminEmail = configuration["Authentication:Admin:Email"];
        var adminPassword = configuration["Authentication:Admin:Password"];
        if (string.IsNullOrWhiteSpace(adminEmail) && string.IsNullOrWhiteSpace(adminPassword))
        {
            logger.LogWarning(
                "No administrator account is configured. Set Authentication:Admin:Email and Authentication:Admin:Password using a secure configuration provider.");
            return;
        }

        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
        {
            throw new InvalidOperationException(
                "Both Authentication:Admin:Email and Authentication:Admin:Password must be configured to provision an administrator.");
        }

        var admin = await userManager.FindByEmailAsync(adminEmail);
        if (admin is null)
        {
            admin = new IdentityUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
            };

            var createResult = await userManager.CreateAsync(admin, adminPassword);
            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Could not create the configured administrator: {string.Join(", ", createResult.Errors.Select(error => error.Code))}");
            }
        }

        if (!await userManager.IsInRoleAsync(admin, AuthRoles.Administrator))
        {
            var roleResult = await userManager.AddToRoleAsync(admin, AuthRoles.Administrator);
            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Could not assign the administrator role: {string.Join(", ", roleResult.Errors.Select(error => error.Code))}");
            }
        }
    }
}
