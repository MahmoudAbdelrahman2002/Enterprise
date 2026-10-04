using System.Security.Claims;
using Enterprise.Application.Common.Authorization;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Domain.Enums;
using Enterprise.Infrastructure.Identity;
using Enterprise.Infrastructure.Persistence;
using Enterprise.Infrastructure.Persistence.Seed;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Enterprise.IntegrationTests;

[TestFixture]
public sealed class AdminPermissionSeedTests : TestFixtureBase
{
    [Test]
    public async Task EnterpriseAdmin_ReceivesAllCurrentAdminPermissionsAndNoOrderRead()
    {
        using var scope = Factory.Services.CreateScope();
        var accounts = scope.ServiceProvider.GetRequiredService<IUserAccountService>();
        var admin = await accounts.FindByEmailAsync("admin@enterprise.local");
        Assert.That(admin, Is.Not.Null);
        Assert.That(admin!.Permissions, Is.EquivalentTo(PermissionCatalog.GetNamesForPortal(UserType.Admin)));
        Assert.That(admin.Permissions, Does.Contain(Permissions.Services.Update));
        Assert.That(admin.Permissions, Does.Contain(Permissions.Services.Delete));
        Assert.That(admin.Permissions, Does.Not.Contain(Permissions.Orders.Read));
    }

    [Test]
    public async Task Reseed_AddsMissingAdminGrantsAndRemovesOldOrderClaimsIdempotently()
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var adminRole = (await roles.FindByNameAsync(UserAccountService.AdminRoleName))!;
        var serviceClaim = (await roles.GetClaimsAsync(adminRole)).Single(claim => claim.Type == "permission" && claim.Value == Permissions.Services.Update);
        Assert.That((await roles.RemoveClaimAsync(adminRole, serviceClaim)).Succeeded, Is.True);
        foreach (var roleName in new[] { UserAccountService.AdminRoleName, "Support Agent", "Operations Manager" })
        {
            var role = (await roles.FindByNameAsync(roleName))!;
            Assert.That((await roles.AddClaimAsync(role, new Claim("permission", "orders.read"))).Succeeded, Is.True);
        }
        var custom = new ApplicationRole("Custom Admin Seed Probe", UserType.Admin) { Id = Guid.NewGuid() };
        Assert.That((await roles.CreateAsync(custom)).Succeeded, Is.True);
        Assert.That((await roles.AddClaimAsync(custom, new Claim("permission", Permissions.Orders.Read))).Succeeded, Is.True);
        Assert.That((await roles.AddClaimAsync(custom, new Claim("permission", Permissions.Clients.Read))).Succeeded, Is.True);

        await DbSeeder.SynchronizeAdminPermissionsAsync(context, roles);
        await DbSeeder.SynchronizeAdminPermissionsAsync(context, roles);

        var claims = await roles.GetClaimsAsync(adminRole);
        var permissions = claims.Where(claim => claim.Type == "permission").Select(claim => claim.Value).ToArray();
        Assert.That(permissions, Is.EquivalentTo(PermissionCatalog.GetNamesForPortal(UserType.Admin)));
        Assert.That(permissions, Is.Unique);
        var adminIds = context.Roles.Where(role => role.RoleType == UserType.Admin).Select(role => role.Id);
        Assert.That(await context.RoleClaims.AnyAsync(claim => adminIds.Contains(claim.RoleId) && claim.ClaimType == "permission" && claim.ClaimValue!.ToUpper() == "ORDERS.READ"), Is.False);
        Assert.That((await roles.GetClaimsAsync(custom)).Select(claim => claim.Value), Is.EquivalentTo(new[] { Permissions.Clients.Read }));
    }

    [Test]
    public async Task AdminPermissionSync_PreservesProviderOrderPermissions()
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var providerRole = (await roles.FindByNameAsync(UserAccountService.ProviderRoleName))!;
        var before = (await roles.GetClaimsAsync(providerRole)).Select(claim => (claim.Type, claim.Value)).ToArray();
        await DbSeeder.SynchronizeAdminPermissionsAsync(context, roles);
        var after = (await roles.GetClaimsAsync(providerRole)).Select(claim => (claim.Type, claim.Value)).ToArray();
        Assert.That(after, Is.EquivalentTo(before));
        Assert.That(after, Does.Contain(("permission", Permissions.ProviderOrder.Read)));
        Assert.That(after, Does.Contain(("permission", Permissions.ProviderOrder.Update)));
    }
}
