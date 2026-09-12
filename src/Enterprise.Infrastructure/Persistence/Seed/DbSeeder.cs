using System.Security.Claims;
using Enterprise.Domain.Enums;
using Enterprise.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Enterprise.Infrastructure.Persistence.Seed;

public static class DbSeeder
{
    private static readonly string[] AdminPermissions =
    [
        "Products.Read",
        "Products.Create",
        "Products.Update",
        "Products.Delete",
        "ApiKeys.Create",
        "Roles.Manage"
    ];

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

        await EnsureRoleAsync(roleManager, UserAccountService.ClientRoleName, system: true, permissions: ["Products.Read"]);
        await EnsureRoleAsync(roleManager, UserAccountService.AdminRoleName, system: true, permissions: AdminPermissions);
        await SeedAdminUserAsync(userManager, configuration, logger);
        await SeedSampleProductsAsync(context, logger, cancellationToken);

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureRoleAsync(
        RoleManager<ApplicationRole> roleManager,
        string roleName,
        bool system,
        IEnumerable<string> permissions)
    {
        var role = await roleManager.FindByNameAsync(roleName);
        if (role is null)
        {
            role = new ApplicationRole(roleName) { Id = Guid.NewGuid(), IsSystem = system };
            await roleManager.CreateAsync(role);
        }

        var existingClaims = await roleManager.GetClaimsAsync(role);
        foreach (var permission in permissions)
        {
            if (existingClaims.Any(c => c.Type == "permission" && c.Value == permission))
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
            IsActive = true
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

    private sealed record SampleProductSeed(
        string Sku,
        decimal Price,
        int Stock,
        string EnName, string EnDesc, string EnCat,
        string ItName, string ItDesc, string ItCat,
        string ArName, string ArDesc, string ArCat,
        bool Discontinued = false);

    private static async Task SeedSampleProductsAsync(ApplicationDbContext context, ILogger logger, CancellationToken cancellationToken)
    {
        SampleProductSeed[] samples =
        [
            new(
                "SKU-LAPTOP-001", 1499.00m, 25,
                "14\" Developer Laptop", "16GB RAM, 1TB NVMe SSD", "Electronics",
                "Laptop da sviluppatore 14\"", "16 GB RAM, SSD NVMe da 1 TB", "Elettronica",
                "حاسوب محمول للمطورين 14 بوصة", "ذاكرة 16 جيجابايت، SSD NVMe سعة 1 تيرابايت", "إلكترونيات"),
            new(
                "SKU-MONITOR-27", 399.00m, 60,
                "27\" 4K Monitor", "IPS panel, USB-C, 60Hz", "Electronics",
                "Monitor 4K da 27\"", "Pannello IPS, USB-C, 60Hz", "Elettronica",
                "شاشة 4K مقاس 27 بوصة", "لوحة IPS، منفذ USB-C، 60 هرتز", "إلكترونيات"),
            new(
                "SKU-CHAIR-ERG", 249.99m, 15,
                "Ergonomic Office Chair", "Adjustable lumbar support", "Furniture",
                "Sedia da ufficio ergonomica", "Supporto lombare regolabile", "Arredamento",
                "كرسي مكتب مريح", "دعم قطني قابل للتعديل", "أثاث"),
            new(
                "SKU-DESK-STAND", 599.00m, 10,
                "Standing Desk", "Electric height adjustment", "Furniture",
                "Scrivania regolabile in altezza", "Regolazione elettrica dell'altezza", "Arredamento",
                "مكتب واقف", "تعديل كهربائي للارتفاع", "أثاث"),
            new(
                "SKU-KEYBOARD-MX", 129.00m, 100,
                "Mechanical Keyboard", "Hot-swappable switches", "Accessories",
                "Tastiera meccanica", "Switch sostituibili a caldo", "Accessori",
                "لوحة مفاتيح ميكانيكية", "مفاتيح قابلة للاستبدال الساخن", "ملحقات",
                Discontinued: true)
        ];

        var existingProducts = await context.Products
            .Include(p => p.Translations)
            .ToListAsync(cancellationToken);

        if (existingProducts.Count == 0)
        {
            logger.LogInformation("Seeding initial {Count} sample products with en/it/ar translations...", samples.Length);
            foreach (var s in samples)
            {
                var p = new Domain.Entities.Product(s.Sku, s.Price, s.Stock);
                p.UpsertTranslation("en", s.EnName, s.EnDesc, s.EnCat);
                p.UpsertTranslation("it", s.ItName, s.ItDesc, s.ItCat);
                p.UpsertTranslation("ar", s.ArName, s.ArDesc, s.ArCat);
                if (s.Discontinued) p.Discontinue();
                context.Products.Add(p);
            }
        }
        else
        {
            logger.LogInformation("Checking and backfilling translations for {Count} existing products...", existingProducts.Count);
            foreach (var s in samples)
            {
                var existing = existingProducts.FirstOrDefault(p => p.Sku == s.Sku);
                if (existing is null) continue;

                existing.UpsertTranslation("en", s.EnName, s.EnDesc, s.EnCat);
                existing.UpsertTranslation("it", s.ItName, s.ItDesc, s.ItCat);
                existing.UpsertTranslation("ar", s.ArName, s.ArDesc, s.ArCat);
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static string GenerateRandomPassword() =>
        $"{Guid.NewGuid():N}".ToUpperInvariant()[..12] + "!a1";
}
