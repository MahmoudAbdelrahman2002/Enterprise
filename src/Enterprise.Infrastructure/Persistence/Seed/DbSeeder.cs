using System.Security.Claims;
using Enterprise.Application.Common.Authorization;
using Enterprise.Domain.Common;
using Enterprise.Domain.Entities;
using Enterprise.Domain.Enums;
using Enterprise.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Enterprise.Infrastructure.Persistence.Seed;

public static class DbSeeder
{
    public static async Task SeedAsync(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IConfiguration configuration,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        if (context.Database.IsSqlServer())
        {
            await context.Database.MigrateAsync(cancellationToken);
        }
        else
        {
            await context.Database.EnsureCreatedAsync(cancellationToken);
        }

        await EnsureRoleAsync(roleManager, UserAccountService.ClientRoleName, UserType.Client, system: true, permissions: []);
        await EnsureRoleAsync(roleManager, UserAccountService.AdminRoleName, UserType.Admin, system: true, permissions: PermissionCatalog.GetNamesForPortal(UserType.Admin));
        await EnsureRoleAsync(roleManager, UserAccountService.ProviderRoleName, UserType.Provider, system: true, permissions: PermissionCatalog.GetNamesForPortal(UserType.Provider));

        await SeedAdminUserAsync(userManager, configuration, logger);
        await SeedMarketplaceServicesAsync(context, logger, cancellationToken);
        await SeedSampleProviderAsync(context, userManager, configuration, logger, cancellationToken);

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureRoleAsync(
        RoleManager<ApplicationRole> roleManager,
        string roleName,
        UserType roleType,
        bool system,
        IEnumerable<string> permissions)
    {
        var role = await roleManager.FindByNameAsync(roleName);
        if (role is null)
        {
            role = new ApplicationRole(roleName, roleType) { Id = Guid.NewGuid(), IsSystem = system };
            await roleManager.CreateAsync(role);
        }
        else
        {
            var needsUpdate = false;
            if (role.IsSystem != system)
            {
                role.IsSystem = system;
                needsUpdate = true;
            }
            if (role.RoleType != roleType)
            {
                role.RoleType = roleType;
                needsUpdate = true;
            }
            if (needsUpdate)
            {
                await roleManager.UpdateAsync(role);
            }
        }

        var existingClaims = await roleManager.GetClaimsAsync(role);

        var legacyRoleManage = existingClaims.FirstOrDefault(c => c.Type == "permission" && c.Value == "Roles.Manage");
        if (legacyRoleManage is not null)
        {
            await roleManager.RemoveClaimAsync(role, legacyRoleManage);
        }

        foreach (var obsoleteProductClaim in existingClaims
            .Where(c => c.Type == "permission" && c.Value is not null && c.Value.StartsWith("Products.", StringComparison.OrdinalIgnoreCase))
            .ToList())
        {
            await roleManager.RemoveClaimAsync(role, obsoleteProductClaim);
        }

        foreach (var permission in permissions)
        {
            if (existingClaims.Any(c => c.Type == "permission" && string.Equals(c.Value, permission, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            await roleManager.AddClaimAsync(role, new Claim("permission", permission));
        }
    }

    private static async Task SeedAdminUserAsync(
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration,
        ILogger logger)
    {
        var adminEmail = configuration["SeedData:AdminEmail"] ?? "admin@enterprise.local";
        var adminPassword = configuration["SeedData:AdminPassword"] ?? GenerateRandomPassword();
        var firstName = string.IsNullOrWhiteSpace(configuration["SeedData:AdminFirstName"])
            ? "System"
            : configuration["SeedData:AdminFirstName"]!;
        var lastName = string.IsNullOrWhiteSpace(configuration["SeedData:AdminLastName"])
            ? "Administrator"
            : configuration["SeedData:AdminLastName"]!;

        var existing = await userManager.FindByEmailAsync(adminEmail);
        if (existing is not null)
        {
            existing.FirstName = firstName;
            existing.LastName = lastName;
            existing.EmailConfirmed = true;
            existing.IsActive = true;
            existing.UserType = UserType.Admin;
            existing.IsSystem = true;
            existing.ProviderId = null;
            await userManager.UpdateAsync(existing);

            if (!string.IsNullOrWhiteSpace(configuration["SeedData:AdminPassword"]))
            {
                var resetToken = await userManager.GeneratePasswordResetTokenAsync(existing);
                await userManager.ResetPasswordAsync(existing, resetToken, adminPassword);
            }

            if (!await userManager.IsInRoleAsync(existing, UserAccountService.AdminRoleName))
            {
                await userManager.AddToRoleAsync(existing, UserAccountService.AdminRoleName);
            }

            return;
        }

        var admin = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true,
            FirstName = firstName,
            LastName = lastName,
            UserType = UserType.Admin,
            IsActive = true,
            IsSystem = true
        };

        var result = await userManager.CreateAsync(admin, adminPassword);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to seed admin user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        await userManager.AddToRoleAsync(admin, UserAccountService.AdminRoleName);

        if (configuration["SeedData:AdminPassword"] is null)
        {
            logger.LogWarning(
                "Seeded default admin account {Email} with a generated password: {Password}. " +
                "Set SeedData:AdminPassword in configuration to control this in real environments.",
                adminEmail, adminPassword);
        }
        else
        {
            logger.LogInformation(
                "Seeded admin account {Email} ({FirstName} {LastName}).",
                adminEmail, firstName, lastName);
        }
    }

    private static async Task SeedMarketplaceServicesAsync(
        ApplicationDbContext context,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var seeds = new (string Code, int DisplayOrder, string En, string It, string Ar, string? EnDesc)[]
        {
            ("restaurant", 1, "Restaurant", "Ristorante", "مطعم", "Food and dining providers"),
            ("pharmacy", 2, "Pharmacy", "Farmacia", "صيدلية", "Pharmacy and health providers"),
        };

        foreach (var seed in seeds)
        {
            var exists = await context.MarketplaceServices
                .IgnoreQueryFilters()
                .AnyAsync(s => s.Code == seed.Code, cancellationToken);

            if (exists)
            {
                continue;
            }

            var service = new MarketplaceService(seed.Code, seed.DisplayOrder, isActive: true);
            service.UpsertTranslation(SupportedLanguages.English, seed.En, seed.EnDesc);
            service.UpsertTranslation(SupportedLanguages.Italian, seed.It, seed.EnDesc);
            service.UpsertTranslation(SupportedLanguages.Arabic, seed.Ar, seed.EnDesc);

            context.MarketplaceServices.Add(service);
            logger.LogInformation("Seeded marketplace service {Code}.", seed.Code);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedSampleProviderAsync(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var email = configuration["SeedData:ProviderEmail"] ?? "provider@enterprise.local";
        var password = configuration["SeedData:ProviderPassword"] ?? "Provider@12345!";
        var firstName = configuration["SeedData:ProviderFirstName"] ?? "Demo";
        var lastName = configuration["SeedData:ProviderLastName"] ?? "Provider";
        var companyName = configuration["SeedData:ProviderCompanyName"] ?? "Demo Restaurant";
        var phone = configuration["SeedData:ProviderPhone"];
        var serviceCode = (configuration["SeedData:ProviderServiceCode"] ?? "restaurant").Trim().ToLowerInvariant();

        var restaurant = await context.MarketplaceServices
            .FirstOrDefaultAsync(s => s.Code == serviceCode && s.IsActive, cancellationToken)
            ?? await context.MarketplaceServices.FirstOrDefaultAsync(s => s.IsActive, cancellationToken);

        var existingUser = await userManager.FindByEmailAsync(email);
        if (existingUser is not null)
        {
            var existingProvider = await context.Providers
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.UserId == existingUser.Id, cancellationToken);

            if (existingProvider is not null)
            {
                // Match CreateProvider: link is Provider.UserId only; AspNetUsers.ProviderId stays null.
                if (existingUser.ProviderId is not null)
                {
                    existingUser.ProviderId = null;
                    await userManager.UpdateAsync(existingUser);
                }

                logger.LogInformation("Sample provider already present for {Email}.", email);
                return;
            }
        }

        ApplicationUser user;
        if (existingUser is null)
        {
            user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FirstName = firstName,
                LastName = lastName,
                UserType = UserType.Provider,
                IsActive = true,
                IsSystem = false,
                ProviderId = null
            };

            var createResult = await userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to seed provider user: {string.Join(", ", createResult.Errors.Select(e => e.Description))}");
            }

            if (!await userManager.IsInRoleAsync(user, UserAccountService.ProviderRoleName))
            {
                await userManager.AddToRoleAsync(user, UserAccountService.ProviderRoleName);
            }
        }
        else
        {
            user = existingUser;
            if (!await userManager.IsInRoleAsync(user, UserAccountService.ProviderRoleName))
            {
                await userManager.AddToRoleAsync(user, UserAccountService.ProviderRoleName);
            }
        }

        var provider = new Provider(
            user.Id,
            companyName,
            string.IsNullOrWhiteSpace(phone) ? null : phone.Trim(),
            restaurant?.Id);

        context.Providers.Add(provider);
        await context.SaveChangesAsync(cancellationToken);

        user.UserType = UserType.Provider;
        user.IsActive = true;
        user.EmailConfirmed = true;
        user.ProviderId = null;
        await userManager.UpdateAsync(user);

        logger.LogInformation(
            "Seeded sample provider {Email} / company {Company} linked to service {ServiceCode}.",
            email,
            companyName,
            restaurant?.Code ?? "(none)");
    }

    private static string GenerateRandomPassword() =>
        $"{Guid.NewGuid():N}".ToUpperInvariant()[..12] + "!a1";
}
