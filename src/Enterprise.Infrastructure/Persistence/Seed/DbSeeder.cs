using System.Security.Claims;
using Enterprise.Application.Common.Authorization;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Common.Models;
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
    private const string DefaultStaffPassword = "Staff@12345!";
    private const string DefaultClientPassword = "Client@12345!";
    private const string DefaultAdminStaffPassword = "Admin@12345!";

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
        await SynchronizeAdminPermissionsAsync(context, roleManager, cancellationToken);
        await EnsureRoleAsync(roleManager, UserAccountService.ProviderRoleName, UserType.Provider, system: true, permissions: PermissionCatalog.GetNamesForPortal(UserType.Provider));
        context.ChangeTracker.Clear();
        await EnsureSystemRoleTranslationsAsync(context, roleManager);
        await context.SaveChangesAsync(cancellationToken);

        await SeedAdminUserAsync(userManager, configuration, logger);
        await SeedMarketplaceServicesAsync(context, logger, cancellationToken);
        await SeedCustomAdminRolesAndStaffAsync(context, userManager, roleManager, logger, cancellationToken);

        var restaurantProviderId = await SeedSampleProviderAsync(context, userManager, configuration, logger, cancellationToken);
        var pharmacyProviderId = await SeedPharmacyProviderAsync(context, userManager, logger, cancellationToken);

        if (restaurantProviderId.HasValue)
        {
            await SeedProviderRolesAndStaffAsync(
                context,
                userManager,
                roleManager,
                restaurantProviderId.Value,
                logger,
                cancellationToken);
        }

        if (pharmacyProviderId.HasValue)
        {
            await SeedPharmacyStaffAsync(
                context,
                userManager,
                roleManager,
                pharmacyProviderId.Value,
                logger,
                cancellationToken);
        }

        await SeedSampleClientsAsync(userManager, configuration, logger);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Demo seed complete. Logins: admin@enterprise.local, support@enterprise.local, ops@enterprise.local, " +
            "provider@enterprise.local, pharmacy@enterprise.local, manager@demo-restaurant.local, " +
            "cashier@demo-restaurant.local, clerk@green-pharmacy.local, client@enterprise.local " +
            "(and client2/client3). Default passwords use the *Pattern@12345! style from SeedData.");
    }

    public static async Task SynchronizeAdminPermissionsAsync(
        ApplicationDbContext context,
        RoleManager<ApplicationRole> roleManager,
        CancellationToken cancellationToken = default)
    {
        await EnsureRoleAsync(
            roleManager,
            UserAccountService.AdminRoleName,
            UserType.Admin,
            system: true,
            permissions: PermissionCatalog.GetNamesForPortal(UserType.Admin));

        // Removing a permission from the catalogue does not remove its persisted claims.
        // Reconcile this retired Admin grant for existing system and custom roles too.
        var adminRoleIds = context.Roles.Where(role => role.RoleType == UserType.Admin).Select(role => role.Id);
        var retiredOrderPermission = Permissions.Orders.Read.ToUpperInvariant();
        var retiredClaims = await context.RoleClaims
            .Where(claim => adminRoleIds.Contains(claim.RoleId)
                && claim.ClaimType == "permission"
                && claim.ClaimValue != null
                && claim.ClaimValue.ToUpper() == retiredOrderPermission)
            .ToListAsync(cancellationToken);
        context.RoleClaims.RemoveRange(retiredClaims);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureRoleAsync(
        RoleManager<ApplicationRole> roleManager,
        string roleName,
        UserType roleType,
        bool system,
        IEnumerable<string> permissions,
        Guid? providerId = null)
    {
        var role = await roleManager.FindByNameAsync(roleName);
        if (role is null)
        {
            role = new ApplicationRole(roleName, roleType, providerId) { Id = Guid.NewGuid(), IsSystem = system };
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
            if (role.ProviderId != providerId)
            {
                role.ProviderId = providerId;
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

    private static async Task EnsureSystemRoleTranslationsAsync(
        ApplicationDbContext context,
        RoleManager<ApplicationRole> roleManager)
    {
        await EnsureRoleTranslationsAsync(context, roleManager, UserAccountService.AdminRoleName, new LocalizedText("Admin", "Amministratore", "مدير"));
        await EnsureRoleTranslationsAsync(context, roleManager, UserAccountService.ProviderRoleName, new LocalizedText("Provider", "Fornitore", "مزود"));
        await EnsureRoleTranslationsAsync(context, roleManager, UserAccountService.ClientRoleName, new LocalizedText("Client", "Cliente", "عميل"));
    }

    private static async Task EnsureRoleTranslationsAsync(
        ApplicationDbContext context,
        RoleManager<ApplicationRole> roleManager,
        string roleName,
        LocalizedText names)
    {
        var role = await roleManager.FindByNameAsync(roleName);
        if (role is null)
        {
            return;
        }

        var existing = await context.RoleTranslations.Where(t => t.RoleId == role.Id).ToListAsync();
        LocalizedContentHelper.Apply((language, value, _) =>
        {
            if (existing.Any(t => t.LanguageCode == language))
            {
                return;
            }

            context.RoleTranslations.Add(new RoleTranslation
            {
                RoleId = role.Id,
                LanguageCode = language,
                Name = value.Trim()
            });
        }, names, description: null);
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
        var seeds = new (
            string Code,
            int DisplayOrder,
            string En,
            string It,
            string Ar,
            string EnDesc,
            string ItDesc,
            string ArDesc)[]
        {
            ("restaurant", 1, "Restaurant", "Ristorante", "مطعم",
                "Food and dining providers", "Fornitori di cibo e ristorazione", "مزودو الطعام والمطاعم"),
            ("pharmacy", 2, "Pharmacy", "Farmacia", "صيدلية",
                "Pharmacy and health providers", "Farmacie e servizi sanitari", "الصيدليات ومزودو الصحة"),
            ("grocery", 3, "Grocery", "Alimentari", "بقالة",
                "Grocery and supermarket providers", "Negozio di alimentari e supermercati", "البقالة والسوبرماركت"),
            ("laundry", 4, "Laundry", "Lavanderia", "مغسلة",
                "Laundry and dry-cleaning providers", "Lavanderia e stireria", "مغاسل وتنظيف الملابس"),
        };

        foreach (var seed in seeds)
        {
            var service = await context.MarketplaceServices
                .IgnoreQueryFilters()
                .Include(s => s.Translations)
                .FirstOrDefaultAsync(s => s.Code == seed.Code, cancellationToken);

            if (service is null)
            {
                service = new MarketplaceService(seed.Code, seed.DisplayOrder, isActive: true);
                context.MarketplaceServices.Add(service);
                logger.LogInformation("Seeded marketplace service {Code}.", seed.Code);
            }

            service.UpsertTranslation(SupportedLanguages.English, seed.En, seed.EnDesc);
            service.UpsertTranslation(SupportedLanguages.Italian, seed.It, seed.ItDesc);
            service.UpsertTranslation(SupportedLanguages.Arabic, seed.Ar, seed.ArDesc);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedCustomAdminRolesAndStaffAsync(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var supportRole = await EnsureCustomRoleAsync(
            context,
            roleManager,
            "Support Agent",
            UserType.Admin,
            providerId: null,
            new LocalizedText("Support Agent", "Agente di supporto", "وكيل الدعم"),
            [
                Permissions.Clients.Read,
                Permissions.Clients.Update,
                Permissions.Providers.Read,
                Permissions.Services.Read
            ],
            cancellationToken);

        var opsRole = await EnsureCustomRoleAsync(
            context,
            roleManager,
            "Operations Manager",
            UserType.Admin,
            providerId: null,
            new LocalizedText("Operations Manager", "Responsabile operativo", "مدير العمليات"),
            [
                Permissions.Providers.Read,
                Permissions.Providers.Create,
                Permissions.Providers.Update,
                Permissions.Services.Read,
                Permissions.Services.Create,
                Permissions.Services.Update,
                Permissions.Clients.Read,
                Permissions.Admins.Read
            ],
            cancellationToken);

        await EnsureStaffUserAsync(
            userManager,
            email: "support@enterprise.local",
            password: DefaultAdminStaffPassword,
            firstName: "Sara",
            lastName: "Support",
            phone: "+201000000010",
            portal: UserType.Admin,
            providerId: null,
            roleName: supportRole.Name!,
            logger);

        await EnsureStaffUserAsync(
            userManager,
            email: "ops@enterprise.local",
            password: DefaultAdminStaffPassword,
            firstName: "Omar",
            lastName: "Operations",
            phone: "+201000000011",
            portal: UserType.Admin,
            providerId: null,
            roleName: opsRole.Name!,
            logger);

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task<Guid?> SeedSampleProviderAsync(
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
                if (existingUser.ProviderId is not null)
                {
                    existingUser.ProviderId = null;
                    await userManager.UpdateAsync(existingUser);
                }

                logger.LogInformation(
                    "Sample provider already present for {Email} (ProviderId={ProviderId}).",
                    email,
                    existingProvider.Id);
                await SeedRestaurantCatalogAsync(context, existingProvider.Id, logger, cancellationToken);
                return existingProvider.Id;
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
            "Seeded sample provider {Email} / company {Company} (ProviderId={ProviderId}) linked to service {ServiceCode}.",
            email,
            companyName,
            provider.Id,
            restaurant?.Code ?? "(none)");

        await SeedRestaurantCatalogAsync(context, provider.Id, logger, cancellationToken);
        return provider.Id;
    }

    private static async Task<Guid?> SeedPharmacyProviderAsync(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        const string email = "pharmacy@enterprise.local";
        const string password = "Provider@12345!";
        const string companyName = "Green Pharmacy";

        var pharmacyService = await context.MarketplaceServices
            .FirstOrDefaultAsync(s => s.Code == "pharmacy" && s.IsActive, cancellationToken);

        var existingUser = await userManager.FindByEmailAsync(email);
        if (existingUser is not null)
        {
            var existingProvider = await context.Providers
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.UserId == existingUser.Id, cancellationToken);

            if (existingProvider is not null)
            {
                var restored = false;
                if (existingProvider.IsDeleted)
                {
                    existingProvider.IsDeleted = false;
                    existingProvider.DeletedAtUtc = null;
                    existingProvider.DeletedBy = null;
                    existingProvider.UpdateDetails(companyName, "+201000000020", pharmacyService?.Id);
                    restored = true;
                }

                if (!existingUser.IsActive
                    || existingUser.UserType != UserType.Provider
                    || existingUser.ProviderId is not null
                    || !existingUser.EmailConfirmed)
                {
                    existingUser.IsActive = true;
                    existingUser.UserType = UserType.Provider;
                    existingUser.EmailConfirmed = true;
                    existingUser.ProviderId = null;
                    await userManager.UpdateAsync(existingUser);
                }

                if (!await userManager.IsInRoleAsync(existingUser, UserAccountService.ProviderRoleName))
                {
                    await userManager.AddToRoleAsync(existingUser, UserAccountService.ProviderRoleName);
                }

                if (restored)
                {
                    await context.SaveChangesAsync(cancellationToken);
                    logger.LogInformation(
                        "Restored soft-deleted pharmacy provider {Email} (ProviderId={ProviderId}).",
                        email,
                        existingProvider.Id);
                }

                await SeedPharmacyCatalogAsync(context, existingProvider.Id, logger, cancellationToken);
                return existingProvider.Id;
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
                FirstName = "Layla",
                LastName = "Pharmacist",
                UserType = UserType.Provider,
                IsActive = true,
                IsSystem = false,
                ProviderId = null,
                PhoneNumber = "+201000000020"
            };

            var createResult = await userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to seed pharmacy provider: {string.Join(", ", createResult.Errors.Select(e => e.Description))}");
            }

            await userManager.AddToRoleAsync(user, UserAccountService.ProviderRoleName);
        }
        else
        {
            user = existingUser;
            if (!await userManager.IsInRoleAsync(user, UserAccountService.ProviderRoleName))
            {
                await userManager.AddToRoleAsync(user, UserAccountService.ProviderRoleName);
            }
        }

        var provider = new Provider(user.Id, companyName, "+201000000020", pharmacyService?.Id);
        context.Providers.Add(provider);
        await context.SaveChangesAsync(cancellationToken);

        user.UserType = UserType.Provider;
        user.IsActive = true;
        user.EmailConfirmed = true;
        user.ProviderId = null;
        await userManager.UpdateAsync(user);

        logger.LogInformation(
            "Seeded pharmacy provider {Email} (ProviderId={ProviderId}).",
            email,
            provider.Id);

        await SeedPharmacyCatalogAsync(context, provider.Id, logger, cancellationToken);
        return provider.Id;
    }

    private static async Task SeedRestaurantCatalogAsync(
        ApplicationDbContext context,
        Guid providerId,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var pizza = await EnsureCategoryAsync(
            context, providerId, 1, "Pizza", "Pizza", "بيتزا",
            "Wood-fired pizzas", "Pizze al forno a legna", "بيتزا فرن حطب", cancellationToken);

        var drinks = await EnsureCategoryAsync(
            context, providerId, 2, "Drinks", "Bevande", "مشروبات",
            "Soft drinks and water", "Bibite e acqua", "مشروبات غازية وماء", cancellationToken);

        var pasta = await EnsureCategoryAsync(
            context, providerId, 3, "Pasta", "Pasta", "باستا",
            "Fresh pasta dishes", "Piatti di pasta fresca", "أطباق باستا طازجة", cancellationToken);

        var desserts = await EnsureCategoryAsync(
            context, providerId, 4, "Desserts", "Dolci", "حلويات",
            "House desserts", "Dolci della casa", "حلويات المنزل", cancellationToken);

        await context.SaveChangesAsync(cancellationToken);

        var productSeeds = new[]
        {
            (CategoryId: pizza.Id, Sku: "PIZZA-MARG", Price: 12.50m,
                En: "Margherita", It: "Margherita", Ar: "مارغريتا",
                EnDesc: "Tomato, mozzarella, basil", ItDesc: "Pomodoro, mozzarella, basilico", ArDesc: "طماطم وموزاريلا وريحان"),
            (CategoryId: pizza.Id, Sku: "PIZZA-PEP", Price: 14.00m,
                En: "Pepperoni", It: "Pepperoni", Ar: "بيبروني",
                EnDesc: "Tomato, mozzarella, pepperoni", ItDesc: "Pomodoro, mozzarella, salame piccante", ArDesc: "طماطم وموزاريلا وبيبروني"),
            (CategoryId: pizza.Id, Sku: "PIZZA-FOUR", Price: 15.50m,
                En: "Quattro Formaggi", It: "Quattro Formaggi", Ar: "أربعة أجبان",
                EnDesc: "Four cheese pizza", ItDesc: "Pizza ai quattro formaggi", ArDesc: "بيتزا بأربعة أجبان"),
            (CategoryId: drinks.Id, Sku: "DRINK-COLA", Price: 3.50m,
                En: "Cola", It: "Cola", Ar: "كولا",
                EnDesc: "Chilled cola 33cl", ItDesc: "Cola fredda 33cl", ArDesc: "كولا باردة 33 مل"),
            (CategoryId: drinks.Id, Sku: "DRINK-WATER", Price: 2.00m,
                En: "Still Water", It: "Acqua naturale", Ar: "مياه معدنية",
                EnDesc: "Still water 50cl", ItDesc: "Acqua naturale 50cl", ArDesc: "مياه معدنية 50 مل"),
            (CategoryId: drinks.Id, Sku: "DRINK-OJ", Price: 4.00m,
                En: "Orange Juice", It: "Succo d'arancia", Ar: "عصير برتقال",
                EnDesc: "Fresh orange juice", ItDesc: "Succo d'arancia fresco", ArDesc: "عصير برتقال طازج"),
            (CategoryId: pasta.Id, Sku: "PASTA-BOL", Price: 11.00m,
                En: "Spaghetti Bolognese", It: "Spaghetti alla Bolognese", Ar: "سباغيتي بولونيز",
                EnDesc: "Beef ragu pasta", ItDesc: "Pasta al ragù di manzo", ArDesc: "باستا بصلصة اللحم"),
            (CategoryId: pasta.Id, Sku: "PASTA-CARB", Price: 12.00m,
                En: "Carbonara", It: "Carbonara", Ar: "كاربونارا",
                EnDesc: "Egg, pecorino, guanciale", ItDesc: "Uovo, pecorino, guanciale", ArDesc: "بيض وبيكورينو وغوانشيالي"),
            (CategoryId: desserts.Id, Sku: "DESS-TIRA", Price: 6.50m,
                En: "Tiramisu", It: "Tiramisù", Ar: "تيراميسو",
                EnDesc: "Classic tiramisu", ItDesc: "Tiramisù classico", ArDesc: "تيراميسو كلاسيك"),
            (CategoryId: desserts.Id, Sku: "DESS-PANA", Price: 5.50m,
                En: "Panna Cotta", It: "Panna cotta", Ar: "بانا كوتا",
                EnDesc: "Vanilla panna cotta", ItDesc: "Panna cotta alla vaniglia", ArDesc: "بانا كوتا بالفانيليا"),
        };

        var addedProducts = await EnsureProductsAsync(context, productSeeds, cancellationToken);

        logger.LogInformation(
            "Restaurant catalog ready for {ProviderId}: categories=4, newlyAddedProducts={AddedProducts}.",
            providerId,
            addedProducts);
    }

    private static async Task SeedPharmacyCatalogAsync(
        ApplicationDbContext context,
        Guid providerId,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var otc = await EnsureCategoryAsync(
            context, providerId, 1, "OTC Medicines", "Farmaci da banco", "أدوية بدون وصفة",
            "Over-the-counter medicines", "Farmaci da banco", "أدوية بدون وصفة طبية", cancellationToken);

        var personalCare = await EnsureCategoryAsync(
            context, providerId, 2, "Personal Care", "Cura della persona", "العناية الشخصية",
            "Hygiene and personal care", "Igiene e cura della persona", "النظافة والعناية الشخصية", cancellationToken);

        var vitamins = await EnsureCategoryAsync(
            context, providerId, 3, "Vitamins", "Vitamine", "فيتامينات",
            "Supplements and vitamins", "Integratori e vitamine", "مكملات وفيتامينات", cancellationToken);

        await context.SaveChangesAsync(cancellationToken);

        var productSeeds = new[]
        {
            (CategoryId: otc.Id, Sku: "PH-PARA", Price: 4.50m,
                En: "Paracetamol 500mg", It: "Paracetamolo 500mg", Ar: "باراسيتامول 500 مجم",
                EnDesc: "Pain and fever relief", ItDesc: "Antidolore e antipiretico", ArDesc: "مسكن وخافض للحرارة"),
            (CategoryId: otc.Id, Sku: "PH-IBU", Price: 5.20m,
                En: "Ibuprofen 400mg", It: "Ibuprofene 400mg", Ar: "إيبوبروفين 400 مجم",
                EnDesc: "Anti-inflammatory tablets", ItDesc: "Compresse antinfiammatorie", ArDesc: "أقراص مضادة للالتهاب"),
            (CategoryId: personalCare.Id, Sku: "PH-SOAP", Price: 3.00m,
                En: "Antibacterial Soap", It: "Sapone antibatterico", Ar: "صابون مضاد للبكتيريا",
                EnDesc: "Hand soap 250ml", ItDesc: "Sapone mani 250ml", ArDesc: "صابون يدين 250 مل"),
            (CategoryId: personalCare.Id, Sku: "PH-TOOTH", Price: 4.80m,
                En: "Toothpaste", It: "Dentifricio", Ar: "معجون أسنان",
                EnDesc: "Fluoride toothpaste", ItDesc: "Dentifricio al fluoro", ArDesc: "معجون أسنان بالفلورايد"),
            (CategoryId: vitamins.Id, Sku: "PH-VITC", Price: 8.90m,
                En: "Vitamin C", It: "Vitamina C", Ar: "فيتامين سي",
                EnDesc: "1000mg tablets", ItDesc: "Compresse da 1000mg", ArDesc: "أقراص 1000 مجم"),
            (CategoryId: vitamins.Id, Sku: "PH-D3", Price: 9.50m,
                En: "Vitamin D3", It: "Vitamina D3", Ar: "فيتامين د3",
                EnDesc: "Immune support drops", ItDesc: "Gocce per il sistema immunitario", ArDesc: "نقط لدعم المناعة"),
        };

        var addedProducts = await EnsureProductsAsync(context, productSeeds, cancellationToken);

        logger.LogInformation(
            "Pharmacy catalog ready for {ProviderId}: categories=3, newlyAddedProducts={AddedProducts}.",
            providerId,
            addedProducts);
    }

    private static async Task SeedProviderRolesAndStaffAsync(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        Guid providerId,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var managerRole = await EnsureCustomRoleAsync(
            context,
            roleManager,
            "Restaurant Manager",
            UserType.Provider,
            providerId,
            new LocalizedText("Store Manager", "Responsabile negozio", "مدير المتجر"),
            PermissionCatalog.GetNamesForPortal(UserType.Provider),
            cancellationToken);

        var cashierRole = await EnsureCustomRoleAsync(
            context,
            roleManager,
            "Restaurant Cashier",
            UserType.Provider,
            providerId,
            new LocalizedText("Cashier", "Cassiere", "أمين الصندوق"),
            [
                Permissions.ProviderOrder.Read,
                Permissions.ProviderOrder.Update,
                Permissions.ProviderProduct.Read,
                Permissions.ProviderCategory.Read
            ],
            cancellationToken);

        await EnsureStaffUserAsync(
            userManager,
            email: "manager@demo-restaurant.local",
            password: DefaultStaffPassword,
            firstName: "Marco",
            lastName: "Manager",
            phone: "+201000000030",
            portal: UserType.Provider,
            providerId: providerId,
            roleName: managerRole.Name!,
            logger);

        await EnsureStaffUserAsync(
            userManager,
            email: "cashier@demo-restaurant.local",
            password: DefaultStaffPassword,
            firstName: "Giulia",
            lastName: "Cashier",
            phone: "+201000000031",
            portal: UserType.Provider,
            providerId: providerId,
            roleName: cashierRole.Name!,
            logger);

        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seeded restaurant staff roles and users for provider {ProviderId}.", providerId);
    }

    private static async Task SeedPharmacyStaffAsync(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        Guid providerId,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var clerkRole = await EnsureCustomRoleAsync(
            context,
            roleManager,
            "Pharmacy Clerk",
            UserType.Provider,
            providerId,
            new LocalizedText("Pharmacy Clerk", "Commesso farmacia", "موظف صيدلية"),
            [
                Permissions.ProviderOrder.Read,
                Permissions.ProviderOrder.Update,
                Permissions.ProviderProduct.Read,
                Permissions.ProviderProduct.Update,
                Permissions.ProviderCategory.Read
            ],
            cancellationToken);

        await EnsureStaffUserAsync(
            userManager,
            email: "clerk@green-pharmacy.local",
            password: DefaultStaffPassword,
            firstName: "Nour",
            lastName: "Clerk",
            phone: "+201000000040",
            portal: UserType.Provider,
            providerId: providerId,
            roleName: clerkRole.Name!,
            logger);

        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seeded pharmacy staff for provider {ProviderId}.", providerId);
    }

    private static async Task SeedSampleClientsAsync(
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration,
        ILogger logger)
    {
        var primaryEmail = configuration["SeedData:ClientEmail"] ?? "client@enterprise.local";
        var primaryPassword = configuration["SeedData:ClientPassword"] ?? DefaultClientPassword;
        var primaryFirst = configuration["SeedData:ClientFirstName"] ?? "Demo";
        var primaryLast = configuration["SeedData:ClientLastName"] ?? "Client";

        await EnsureClientUserAsync(userManager, primaryEmail, primaryPassword, primaryFirst, primaryLast, "+201000000100", logger);

        await EnsureClientUserAsync(userManager, "client2@enterprise.local", DefaultClientPassword, "Ahmed", "Hassan", "+201000000101", logger);
        await EnsureClientUserAsync(userManager, "client3@enterprise.local", DefaultClientPassword, "Elena", "Rossi", "+201000000102", logger);
        await EnsureClientUserAsync(userManager, "client4@enterprise.local", DefaultClientPassword, "Fatima", "Ali", "+201000000103", logger);
    }

    private static async Task EnsureClientUserAsync(
        UserManager<ApplicationUser> userManager,
        string email,
        string password,
        string firstName,
        string lastName,
        string? phone,
        ILogger logger)
    {
        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            existing.FirstName = firstName;
            existing.LastName = lastName;
            existing.EmailConfirmed = true;
            // Do not force IsActive — preserve admin deactivate/activate choices across restarts.
            existing.UserType = UserType.Client;
            existing.IsSystem = false;
            existing.ProviderId = null;
            existing.PhoneNumber = phone;
            await userManager.UpdateAsync(existing);

            if (!await userManager.IsInRoleAsync(existing, UserAccountService.ClientRoleName))
            {
                await userManager.AddToRoleAsync(existing, UserAccountService.ClientRoleName);
            }

            logger.LogInformation("Sample client already present for {Email}.", email);
            return;
        }

        var client = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = firstName,
            LastName = lastName,
            PhoneNumber = phone,
            UserType = UserType.Client,
            IsActive = true,
            IsSystem = false,
            ProviderId = null
        };

        var result = await userManager.CreateAsync(client, password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to seed client user {email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        await userManager.AddToRoleAsync(client, UserAccountService.ClientRoleName);
        logger.LogInformation("Seeded sample client {Email}.", email);
    }

    private static async Task<ApplicationRole> EnsureCustomRoleAsync(
        ApplicationDbContext context,
        RoleManager<ApplicationRole> roleManager,
        string roleName,
        UserType roleType,
        Guid? providerId,
        LocalizedText displayNames,
        IEnumerable<string> permissions,
        CancellationToken cancellationToken)
    {
        await EnsureRoleAsync(roleManager, roleName, roleType, system: false, permissions, providerId);
        // RoleManager already SaveChanges'd; clear so AspNetRoles concurrency stamps are not re-saved.
        context.ChangeTracker.Clear();
        await EnsureRoleTranslationsAsync(context, roleManager, roleName, displayNames);
        await context.SaveChangesAsync(cancellationToken);

        var role = await roleManager.FindByNameAsync(roleName)
            ?? throw new InvalidOperationException($"Failed to resolve seeded role '{roleName}'.");
        return role;
    }

    private static async Task EnsureStaffUserAsync(
        UserManager<ApplicationUser> userManager,
        string email,
        string password,
        string firstName,
        string lastName,
        string? phone,
        UserType portal,
        Guid? providerId,
        string roleName,
        ILogger logger)
    {
        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            existing.FirstName = firstName;
            existing.LastName = lastName;
            existing.PhoneNumber = phone;
            existing.EmailConfirmed = true;
            // Do not force IsActive — preserve admin/provider deactivate choices across restarts.
            existing.IsSystem = false;
            existing.UserType = portal;
            existing.ProviderId = providerId;
            await userManager.UpdateAsync(existing);

            var currentRoles = await userManager.GetRolesAsync(existing);
            if (!currentRoles.Contains(roleName))
            {
                if (currentRoles.Count > 0)
                {
                    await userManager.RemoveFromRolesAsync(existing, currentRoles);
                }

                await userManager.AddToRoleAsync(existing, roleName);
            }

            logger.LogInformation("Staff user already present for {Email}.", email);
            return;
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = firstName,
            LastName = lastName,
            PhoneNumber = phone,
            UserType = portal,
            IsActive = true,
            IsSystem = false,
            ProviderId = providerId
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to seed staff user {email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        await userManager.AddToRoleAsync(user, roleName);
        logger.LogInformation("Seeded staff user {Email} with role {Role}.", email, roleName);
    }

    private static async Task<Category> EnsureCategoryAsync(
        ApplicationDbContext context,
        Guid providerId,
        int displayOrder,
        string enName,
        string itName,
        string arName,
        string enDesc,
        string itDesc,
        string arDesc,
        CancellationToken cancellationToken)
    {
        var existing = await context.Categories
            .IgnoreQueryFilters()
            .Include(c => c.Translations)
            .FirstOrDefaultAsync(
                c => c.ProviderId == providerId
                     && c.Translations.Any(t => t.LanguageCode == SupportedLanguages.English && t.Name == enName),
                cancellationToken);

        if (existing is not null)
        {
            if (existing.IsDeleted)
            {
                existing.IsDeleted = false;
                existing.DeletedAtUtc = null;
                existing.DeletedBy = null;
                existing.SetActive(true);
                existing.UpdateDetails(displayOrder);
            }

            return existing;
        }

        var category = new Category(providerId, displayOrder, isActive: true);
        category.UpsertTranslation(SupportedLanguages.English, enName, enDesc);
        category.UpsertTranslation(SupportedLanguages.Italian, itName, itDesc);
        category.UpsertTranslation(SupportedLanguages.Arabic, arName, arDesc);
        context.Categories.Add(category);
        return category;
    }

    private static async Task<int> EnsureProductsAsync(
        ApplicationDbContext context,
        (Guid CategoryId, string Sku, decimal Price, string En, string It, string Ar, string EnDesc, string ItDesc, string ArDesc)[] productSeeds,
        CancellationToken cancellationToken)
    {
        var addedProducts = 0;
        var restoredProducts = 0;
        foreach (var seed in productSeeds)
        {
            var existing = await context.Products
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.CategoryId == seed.CategoryId && p.Sku == seed.Sku, cancellationToken);

            if (existing is not null)
            {
                if (existing.IsDeleted)
                {
                    existing.IsDeleted = false;
                    existing.DeletedAtUtc = null;
                    existing.DeletedBy = null;
                    existing.UpdateDetails(seed.Sku, seed.Price);
                    existing.SetStatus(ProductStatus.Active);
                    restoredProducts++;
                }

                continue;
            }

            context.Products.Add(CreateSeedProduct(
                seed.CategoryId,
                seed.Sku,
                seed.Price,
                seed.En,
                seed.It,
                seed.Ar,
                seed.EnDesc,
                seed.ItDesc,
                seed.ArDesc));
            addedProducts++;
        }

        if (addedProducts > 0 || restoredProducts > 0)
        {
            await context.SaveChangesAsync(cancellationToken);
        }

        return addedProducts + restoredProducts;
    }

    private static Product CreateSeedProduct(
        Guid categoryId,
        string sku,
        decimal price,
        string enName,
        string itName,
        string arName,
        string enDesc,
        string itDesc,
        string arDesc)
    {
        var product = new Product(categoryId, sku, price, ProductStatus.Active);
        product.UpsertTranslation(SupportedLanguages.English, enName, enDesc);
        product.UpsertTranslation(SupportedLanguages.Italian, itName, itDesc);
        product.UpsertTranslation(SupportedLanguages.Arabic, arName, arDesc);
        return product;
    }

    private static string GenerateRandomPassword() =>
        $"{Guid.NewGuid():N}".ToUpperInvariant()[..12] + "!a1";
}
