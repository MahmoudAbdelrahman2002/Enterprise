using System.Net;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Common.Models;
using Enterprise.Application.Common.Settings;
using Enterprise.Domain.Enums;
using Enterprise.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Enterprise.Infrastructure.Identity;

public sealed class StaffManagerService(
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager,
    ApplicationDbContext context,
    IEmailSender emailSender,
    IOptions<DashboardUrlSettings> dashboardUrls,
    ILogger<StaffManagerService> logger) : IStaffManagerService
{
    private readonly DashboardUrlSettings _dashboardUrls = dashboardUrls.Value;

    public async Task<PagedResult<StaffListItemDto>> GetStaffListAsync(
        UserType portal,
        Guid? providerId,
        PaginationParams pagination,
        string? searchTerm = null,
        Guid? roleId = null,
        bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var query = context.Users.AsNoTracking().Where(u => u.UserType == portal);

        if (portal == UserType.Provider)
        {
            query = query.Where(u => u.ProviderId == providerId);
        }
        else
        {
            query = query.Where(u => u.ProviderId == null);
        }

        if (isActive.HasValue)
        {
            query = query.Where(u => u.IsActive == isActive.Value);
        }

        if (roleId.HasValue)
        {
            query = query.Where(u => context.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == roleId.Value));
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var trimmed = searchTerm.Trim();
            query = query.Where(u =>
                (u.Email != null && u.Email.Contains(trimmed)) ||
                u.FirstName.Contains(trimmed) ||
                u.LastName.Contains(trimmed) ||
                (u.PhoneNumber != null && u.PhoneNumber.Contains(trimmed)));
        }

        var totalItems = await query.CountAsync(cancellationToken);

        var users = await query
            .OrderByDescending(u => u.IsSystem)
            .ThenBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);

        var userIds = users.Select(u => u.Id).ToList();

        var rolesByUser = await (
            from ur in context.UserRoles.AsNoTracking()
            join r in context.Roles.AsNoTracking() on ur.RoleId equals r.Id
            where userIds.Contains(ur.UserId)
            select new { ur.UserId, RoleId = r.Id, RoleName = r.Name ?? string.Empty }
        ).ToListAsync(cancellationToken);

        var roleByUserId = rolesByUser
            .GroupBy(x => x.UserId)
            .ToDictionary(g => g.Key, g => g.First());

        var items = users.Select(user =>
        {
            var r = roleByUserId.GetValueOrDefault(user.Id);
            return new StaffListItemDto(
                user.Id,
                user.FirstName,
                user.LastName,
                user.Email ?? string.Empty,
                user.PhoneNumber,
                r?.RoleId ?? Guid.Empty,
                r?.RoleName ?? string.Empty,
                user.UserType,
                user.ProviderId,
                user.IsActive,
                user.IsSystem
            );
        }).ToList();

        return new PagedResult<StaffListItemDto>(items, totalItems, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<StaffDetailDto?> GetStaffByIdAsync(
        Guid staffId,
        UserType portal,
        Guid? providerId,
        CancellationToken cancellationToken = default)
    {
        var query = context.Users.AsNoTracking().Where(u => u.Id == staffId && u.UserType == portal);

        if (portal == UserType.Provider)
        {
            query = query.Where(u => u.ProviderId == providerId);
        }
        else
        {
            query = query.Where(u => u.ProviderId == null);
        }

        var user = await query.FirstOrDefaultAsync(cancellationToken);
        if (user is null)
        {
            return null;
        }

        var userRole = await (
            from ur in context.UserRoles.AsNoTracking()
            join r in context.Roles.AsNoTracking() on ur.RoleId equals r.Id
            where ur.UserId == user.Id
            select new { RoleId = r.Id, RoleName = r.Name ?? string.Empty }
        ).FirstOrDefaultAsync(cancellationToken);

        var permissions = new List<string>();
        if (userRole != null)
        {
            permissions = await context.RoleClaims
                .AsNoTracking()
                .Where(rc => rc.RoleId == userRole.RoleId && rc.ClaimType == "permission" && rc.ClaimValue != null)
                .Select(rc => rc.ClaimValue!)
                .Distinct()
                .ToListAsync(cancellationToken);
        }

        return new StaffDetailDto(
            user.Id,
            user.FirstName,
            user.LastName,
            user.Email ?? string.Empty,
            user.PhoneNumber,
            userRole?.RoleId ?? Guid.Empty,
            userRole?.RoleName ?? string.Empty,
            permissions,
            user.UserType,
            user.ProviderId,
            user.IsActive,
            user.IsSystem
        );
    }

    public async Task<CreateStaffResult> CreateStaffAsync(
        CreateStaffRequest request,
        UserType portal,
        Guid? providerId,
        CancellationToken cancellationToken = default)
    {
        var existing = await userManager.FindByEmailAsync(request.Email.Trim());
        if (existing != null)
        {
            return new CreateStaffResult(false, Error: MessageKeys.Account.EmailExists);
        }

        var role = await roleManager.FindByIdAsync(request.RoleId.ToString());
        if (role == null || role.RoleType != portal)
        {
            return new CreateStaffResult(false, Error: portal == UserType.Admin ? MessageKeys.AdminUser.RoleNotFound : MessageKeys.ProviderStaff.RoleNotFound);
        }

        if (portal == UserType.Provider && role.ProviderId != providerId)
        {
            return new CreateStaffResult(false, Error: MessageKeys.Error.NotFound);
        }

        if (portal == UserType.Admin && role.ProviderId != null)
        {
            return new CreateStaffResult(false, Error: MessageKeys.Error.NotFound);
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = request.Email.Trim(),
            Email = request.Email.Trim(),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim(),
            EmailConfirmed = true,
            IsActive = true,
            IsSystem = false,
            UserType = portal,
            ProviderId = providerId
        };

        var createResult = await userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            var errors = createResult.Errors.ToMessageKeysOrDescriptions();
            return new CreateStaffResult(false, Error: errors.FirstOrDefault(), Errors: errors);
        }

        var addToRoleResult = await userManager.AddToRoleAsync(user, role.Name!);
        if (!addToRoleResult.Succeeded)
        {
            await userManager.DeleteAsync(user);
            var errors = addToRoleResult.Errors.ToMessageKeysOrDescriptions();
            return new CreateStaffResult(false, Error: errors.FirstOrDefault(), Errors: errors);
        }

        var portalTitle = portal == UserType.Admin ? "Subito Admin Portal" : "Subito Merchant Portal";
        var dashboardUrl = portal == UserType.Admin ? _dashboardUrls.Admin : _dashboardUrls.Provider;
        var fullName = $"{user.FirstName} {user.LastName}";
        var subject = $"Welcome to {portalTitle} - Your Login Credentials";
        var htmlBody = BuildWelcomeStaffEmailHtml(fullName, portalTitle, user.Email!, request.Password, dashboardUrl);

        try
        {
            await emailSender.SendAsync(user.Email!, subject, htmlBody, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send welcome credentials email to staff user {Email}. Rolling back creation.", user.Email);
            await userManager.DeleteAsync(user);
            return new CreateStaffResult(false, Error: MessageKeys.Error.EmailDelivery);
        }

        return new CreateStaffResult(true, StaffId: user.Id);
    }

    public async Task<UpdateStaffResult> UpdateStaffAsync(
        Guid staffId,
        UpdateStaffRequest request,
        UserType portal,
        Guid? providerId,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(staffId.ToString());
        if (user == null || user.UserType != portal || (portal == UserType.Provider && user.ProviderId != providerId) || (portal == UserType.Admin && user.ProviderId != null))
        {
            return new UpdateStaffResult(false, Error: MessageKeys.Error.NotFound);
        }

        if (user.IsSystem)
        {
            return new UpdateStaffResult(false, Error: MessageKeys.Role.CannotModifySystemRole);
        }

        var newRole = await roleManager.FindByIdAsync(request.RoleId.ToString());
        if (newRole == null || newRole.RoleType != portal || (portal == UserType.Provider && newRole.ProviderId != providerId) || (portal == UserType.Admin && newRole.ProviderId != null))
        {
            return new UpdateStaffResult(false, Error: portal == UserType.Admin ? MessageKeys.AdminUser.RoleNotFound : MessageKeys.ProviderStaff.RoleNotFound);
        }

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim();

        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            var errors = updateResult.Errors.ToMessageKeysOrDescriptions();
            return new UpdateStaffResult(false, Error: errors.FirstOrDefault(), Errors: errors);
        }

        var currentRoles = await userManager.GetRolesAsync(user);
        if (!currentRoles.Contains(newRole.Name!))
        {
            if (currentRoles.Count > 0)
            {
                await userManager.RemoveFromRolesAsync(user, currentRoles);
            }
            await userManager.AddToRoleAsync(user, newRole.Name!);
        }

        return new UpdateStaffResult(true);
    }

    public async Task<SetActiveResult> SetStaffActiveAsync(
        Guid staffId,
        bool isActive,
        UserType portal,
        Guid? providerId,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(staffId.ToString());
        if (user == null || user.UserType != portal || (portal == UserType.Provider && user.ProviderId != providerId) || (portal == UserType.Admin && user.ProviderId != null))
        {
            return new SetActiveResult(false, Error: MessageKeys.Error.NotFound);
        }

        if (user.IsSystem && !isActive)
        {
            return new SetActiveResult(false, Error: MessageKeys.Role.CannotDeleteSystemUser);
        }

        user.IsActive = isActive;
        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var errors = result.Errors.ToMessageKeysOrDescriptions();
            return new SetActiveResult(false, Error: errors.FirstOrDefault(), Errors: errors);
        }

        return new SetActiveResult(true);
    }

    public async Task<DeleteStaffResult> DeleteStaffAsync(
        Guid staffId,
        UserType portal,
        Guid? providerId,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(staffId.ToString());
        if (user == null || user.UserType != portal || (portal == UserType.Provider && user.ProviderId != providerId) || (portal == UserType.Admin && user.ProviderId != null))
        {
            return new DeleteStaffResult(false, Error: MessageKeys.Error.NotFound);
        }

        if (user.IsSystem)
        {
            return new DeleteStaffResult(false, Error: MessageKeys.Role.CannotDeleteSystemUser);
        }

        var result = await userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            var errors = result.Errors.ToMessageKeysOrDescriptions();
            return new DeleteStaffResult(false, Error: errors.FirstOrDefault(), Errors: errors);
        }

        return new DeleteStaffResult(true);
    }

    private static string BuildWelcomeStaffEmailHtml(
        string fullName,
        string portalTitle,
        string email,
        string password,
        string dashboardUrl)
    {
        return $$"""
        <!DOCTYPE html>
        <html lang="en">
        <head>
          <meta charset="utf-8">
          <meta name="viewport" content="width=device-width, initial-scale=1.0">
          <title>Welcome to {{WebUtility.HtmlEncode(portalTitle)}}</title>
          <style>
            body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; background-color: #f8fafc; margin: 0; padding: 20px; color: #1e293b; }
            .card { max-width: 560px; margin: 0 auto; background: #ffffff; border-radius: 12px; border: 1px solid #e2e8f0; overflow: hidden; box-shadow: 0 4px 6px -1px rgba(0,0,0,0.05); }
            .header { background: #0f172a; padding: 32px 24px; text-align: center; color: #ffffff; }
            .header h1 { margin: 0 0 6px 0; font-size: 24px; font-weight: 700; }
            .header p { margin: 0; font-size: 14px; color: #94a3b8; }
            .content { padding: 32px 24px; }
            .greeting { font-size: 18px; font-weight: 600; margin-bottom: 12px; }
            .intro { font-size: 15px; line-height: 1.6; color: #475569; margin-bottom: 24px; }
            .creds-box { background: #f1f5f9; border-radius: 8px; padding: 18px 20px; margin-bottom: 24px; border: 1px solid #cbd5e1; }
            .cred-row { margin-bottom: 10px; font-size: 14px; }
            .cred-row:last-child { margin-bottom: 0; }
            .cred-label { font-weight: 600; color: #64748b; display: inline-block; width: 90px; }
            .cred-val { font-family: monospace; font-size: 15px; font-weight: 700; color: #0f172a; }
            .btn-container { text-align: center; margin: 30px 0; }
            .btn { display: inline-block; background-color: #2563eb; color: #ffffff !important; text-decoration: none; font-weight: 600; padding: 12px 28px; border-radius: 8px; font-size: 15px; }
            .alert-box { background: #fffbeb; border: 1px solid #fde68a; border-radius: 8px; padding: 14px 16px; font-size: 13px; color: #92400e; line-height: 1.5; }
            .footer { border-top: 1px solid #e2e8f0; padding: 20px 24px; text-align: center; font-size: 12px; color: #94a3b8; }
          </style>
        </head>
        <body>
          <div class="card">
            <div class="header">
              <h1>Subito</h1>
              <p>{{WebUtility.HtmlEncode(portalTitle)}}</p>
            </div>
            <div class="content">
              <div class="greeting">Hello {{WebUtility.HtmlEncode(fullName)}},</div>
              <p class="intro">An account has been created for you to access the <strong>{{WebUtility.HtmlEncode(portalTitle)}}</strong>. Below are your login credentials to get started.</p>
              
              <div class="creds-box">
                <div class="cred-row">
                  <span class="cred-label">Email:</span>
                  <span class="cred-val">{{WebUtility.HtmlEncode(email)}}</span>
                </div>
                <div class="cred-row">
                  <span class="cred-label">Password:</span>
                  <span class="cred-val">{{WebUtility.HtmlEncode(password)}}</span>
                </div>
              </div>

              <div class="btn-container">
                <a href="{{WebUtility.HtmlEncode(dashboardUrl)}}" class="btn" target="_blank">Access Dashboard</a>
              </div>

              <div class="alert-box">
                <strong>Security Notice:</strong> For your security, we strongly recommend changing your password immediately after your first sign in from your account settings.
              </div>
            </div>
            <div class="footer">
              &copy; {{DateTime.UtcNow.Year}} Subito. All rights reserved. If you did not expect this invitation, please contact your administrator.
            </div>
          </div>
        </body>
        </html>
        """;
    }
}
