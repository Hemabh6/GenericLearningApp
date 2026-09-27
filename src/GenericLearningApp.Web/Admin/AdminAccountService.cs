using System.Net.Mail;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using GenericLearningApp.Application.Admin;
using GenericLearningApp.Application.Site;
using GenericLearningApp.Domain.Enums;
using GenericLearningApp.Web.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace GenericLearningApp.Web.Admin;

public sealed record PeopleList(IReadOnlyList<PersonRow> Rows, int Total, int Guests);

/// <summary>
/// Everything the admin area does to accounts and roles. Every method re-reads who is acting from the
/// database, checks the permission it needs, and applies <see cref="AccountRules"/>, so the protections
/// hold no matter which page (or future caller) reaches them. Each call gets its own DI scope because
/// Blazor Server components outlive any single database context.
/// </summary>
public sealed class AdminAccountService(
    IServiceScopeFactory scopes,
    SiteConfigStore store,
    ILogger<AdminAccountService> logger)
{
    private const string DescriptionClaim = "desc";

    private sealed record Ctx(
        UserManager<ApplicationUser> Users,
        RoleManager<IdentityRole> Roles,
        ApplicationDbContext Db,
        ISiteConfigService Site,
        IEmailSender<ApplicationUser> Email);

    private async Task<T> RunAsync<T>(Func<Ctx, Task<T>> work)
    {
        await using var scope = scopes.CreateAsyncScope();
        var sp = scope.ServiceProvider;
        return await work(new Ctx(
            sp.GetRequiredService<UserManager<ApplicationUser>>(),
            sp.GetRequiredService<RoleManager<IdentityRole>>(),
            sp.GetRequiredService<ApplicationDbContext>(),
            sp.GetRequiredService<ISiteConfigService>(),
            sp.GetRequiredService<IEmailSender<ApplicationUser>>()));
    }

    // ---------------------------------------------------------------- who is acting

    /// <summary>The signed-in person's role and permissions, or null when they have none of either.</summary>
    public Task<Actor?> GetActorAsync(ClaimsPrincipal principal)
        => RunAsync(c => ActorAsync(c, principal));

    private static async Task<Actor?> ActorAsync(Ctx c, ClaimsPrincipal principal)
    {
        var id = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (id is null) return null;

        var user = await c.Users.FindByIdAsync(id);
        if (user is null) return null;

        var roles = await c.Users.GetRolesAsync(user);
        var permissions = await PermissionsForAsync(c, roles);
        return new Actor(user.Id, user.Email ?? user.UserName ?? id, EffectiveRole(roles), user.IsSuperAdmin, permissions);
    }

    private static async Task<Actor> RequireAsync(Ctx c, ClaimsPrincipal principal, string permission)
    {
        var actor = await ActorAsync(c, principal);
        if (actor is null || !actor.Has(permission))
            throw new AdminException("You don't have permission to do that.");
        return actor;
    }

    private static async Task<HashSet<string>> PermissionsForAsync(Ctx c, IEnumerable<string> roleNames)
    {
        var set = new HashSet<string>();
        foreach (var name in roleNames)
        {
            var role = await c.Roles.FindByNameAsync(name);
            if (role is null) continue;
            foreach (var claim in await c.Roles.GetClaimsAsync(role))
            {
                if (claim.Type == AdminPermissions.ClaimType && AdminPermissions.IsKnown(claim.Value)) set.Add(claim.Value);
            }
        }
        return set;
    }

    /// <summary>A person has one role. No role means the default, Learner.</summary>
    private static string? EffectiveRole(IEnumerable<string> roles)
    {
        var list = roles.ToList();
        if (list.Any(AccountRules.IsAdmin)) return BuiltInRoles.Admin;
        return list.FirstOrDefault(r => !string.Equals(r, BuiltInRoles.Learner, StringComparison.OrdinalIgnoreCase));
    }

    // ---------------------------------------------------------------- people

    public Task<PeopleList> ListPeopleAsync(ClaimsPrincipal principal, string? search, bool includeGuests, int take = 200)
        => RunAsync(async c =>
        {
            var actor = await RequireAsync(c, principal, AdminPermissions.People);

            var real = c.Db.Users.AsNoTracking().Where(u => u.UserName == null || !u.UserName.StartsWith(GuestAccount.Prefix));
            var total = await real.CountAsync();
            var guests = await c.Db.Users.CountAsync(u => u.UserName != null && u.UserName.StartsWith(GuestAccount.Prefix));

            var query = includeGuests ? c.Db.Users.AsNoTracking() : real;
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(u => (u.Email != null && u.Email.ToLower().Contains(s)) || (u.UserName != null && u.UserName.ToLower().Contains(s)));
            }

            var users = await query.OrderBy(u => u.Email).ThenBy(u => u.UserName).Take(take).ToListAsync();
            var ids = users.Select(u => u.Id).ToList();

            var roleByUser = (await (from ur in c.Db.UserRoles
                                     join r in c.Db.Roles on ur.RoleId equals r.Id
                                     where ids.Contains(ur.UserId)
                                     select new { ur.UserId, r.Name }).ToListAsync())
                .GroupBy(x => x.UserId)
                .ToDictionary(g => g.Key, g => EffectiveRole(g.Select(x => x.Name!)));

            var withLogin = (await c.Db.UserLogins.Where(l => ids.Contains(l.UserId)).Select(l => l.UserId).Distinct().ToListAsync()).ToHashSet();
            var overrides = await c.Site.CountOverridesByUserAsync();

            var rows = users.Select(u =>
            {
                var guest = GuestAccount.IsGuest(u.UserName);
                var status = guest ? "Guest" : (u.PasswordHash is null && !withLogin.Contains(u.Id) ? "Invited" : "Active");
                return new PersonRow(
                    u.Id,
                    guest ? "Guest" : DisplayName(u),
                    u.Email ?? "(no email)",
                    roleByUser.GetValueOrDefault(u.Id),
                    status,
                    u.IsSuperAdmin,
                    u.Id == actor.Id,
                    overrides.GetValueOrDefault(u.Id));
            }).ToList();

            return new PeopleList(rows, total, guests);
        });

    private static string DisplayName(ApplicationUser u)
    {
        var e = u.Email ?? u.UserName ?? "user";
        var at = e.IndexOf('@');
        return at > 0 ? e[..at] : e;
    }

    public Task<InviteResult> InviteAsync(ClaimsPrincipal principal, string? email, string? roleName, string baseUri)
        => RunAsync(async c =>
        {
            var actor = await RequireAsync(c, principal, AdminPermissions.People);

            email = (email ?? string.Empty).Trim();
            if (!IsEmail(email)) throw new AdminException("Enter a valid email address, like name@example.com.");
            if (await c.Users.FindByEmailAsync(email) is not null) throw new AdminException("That email is already on the list.");

            var role = await ResolveRoleAsync(c, roleName);
            if (AccountRules.IsAdmin(role) && !actor.IsAdmin)
                throw new AdminException("Only an admin can give or take away the Admin role.");

            // The admin vouches for the address, so it starts confirmed; the person only has to choose a password.
            var user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
            var created = await c.Users.CreateAsync(user);
            if (!created.Succeeded) throw new AdminException(string.Join(" ", created.Errors.Select(e => e.Description)));

            if (role is not null) await c.Users.AddToRoleAsync(user, role);

            var token = await c.Users.GeneratePasswordResetTokenAsync(user);
            var code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            var link = new Uri(new Uri(baseUri), "Account/ResetPassword?code=" + code).AbsoluteUri;

            // Real delivery arrives with a real email sender; until then the admin passes the link on.
            await c.Email.SendPasswordResetLinkAsync(user, email, HtmlEncoder.Default.Encode(link));

            logger.LogInformation("{Actor} invited {Email} as {Role}.", actor.Email, email, role ?? BuiltInRoles.Learner);
            return new InviteResult(email, link);
        });

    public Task ChangeRoleAsync(ClaimsPrincipal principal, string userId, string? roleName)
        => RunAsync(async c =>
        {
            var actor = await RequireAsync(c, principal, AdminPermissions.People);
            var target = await c.Users.FindByIdAsync(userId) ?? throw new AdminException("That person no longer exists.");

            // Otherwise a lone admin could demote themselves, or an editor promote themselves.
            if (target.Id == actor.Id) throw new AdminException("You can't change your own role.");

            var current = EffectiveRole(await c.Users.GetRolesAsync(target));
            var role = await ResolveRoleAsync(c, roleName);
            var adminCount = (await c.Users.GetUsersInRoleAsync(BuiltInRoles.Admin)).Count;

            var refusal = AccountRules.CheckChangeRole(new PersonInfo(target.Id, current, target.IsSuperAdmin), role, actor.IsAdmin, adminCount);
            if (refusal is not null) throw new AdminException(refusal);

            var existing = await c.Users.GetRolesAsync(target);
            if (existing.Count > 0) await c.Users.RemoveFromRolesAsync(target, existing);
            if (role is not null) await c.Users.AddToRoleAsync(target, role);

            logger.LogInformation("{Actor} set {Target}'s role to {Role}.", actor.Email, target.Email, role ?? BuiltInRoles.Learner);
            return true;
        });

    public Task RemoveAsync(ClaimsPrincipal principal, string userId)
        => RunAsync(async c =>
        {
            var actor = await RequireAsync(c, principal, AdminPermissions.People);
            var target = await c.Users.FindByIdAsync(userId) ?? throw new AdminException("That person no longer exists.");

            var role = EffectiveRole(await c.Users.GetRolesAsync(target));
            var adminCount = (await c.Users.GetUsersInRoleAsync(BuiltInRoles.Admin)).Count;

            var refusal = AccountRules.CheckRemove(new PersonInfo(target.Id, role, target.IsSuperAdmin), actor.Id, actor.IsAdmin, adminCount);
            if (refusal is not null) throw new AdminException(refusal);

            // Their subjects, notes, plans and scores are theirs alone, so they leave with the account.
            await c.Site.RemoveUserAsync(target.Id);
            var deleted = await c.Users.DeleteAsync(target);
            if (!deleted.Succeeded) throw new AdminException(string.Join(" ", deleted.Errors.Select(e => e.Description)));

            logger.LogWarning("{Actor} removed {Target}.", actor.Email, target.Email ?? target.UserName);
            return true;
        });

    public Task SetMenuOverrideAsync(ClaimsPrincipal principal, string userId, Guid menuId, OverrideMode? mode)
        => RunAsync(async c =>
        {
            await RequireAsync(c, principal, AdminPermissions.People);
            if (await c.Users.FindByIdAsync(userId) is null) throw new AdminException("That person no longer exists.");
            await c.Site.SetOverrideAsync(userId, menuId, mode);
            return true;
        });

    public Task<IReadOnlyDictionary<Guid, OverrideMode>> GetOverridesForAsync(ClaimsPrincipal principal, string userId)
        => RunAsync(async c =>
        {
            await RequireAsync(c, principal, AdminPermissions.People);
            return await c.Site.GetOverridesAsync(userId);
        });

    /// <summary>What the navigation needs to know about someone: their role and their menu exceptions.</summary>
    public Task<(MenuViewer Viewer, IReadOnlyDictionary<Guid, OverrideMode> Overrides)> GetViewerAsync(ClaimsPrincipal principal)
        => RunAsync(async c =>
        {
            if (principal.Identity?.IsAuthenticated != true)
                return (MenuViewer.Anonymous, (IReadOnlyDictionary<Guid, OverrideMode>)new Dictionary<Guid, OverrideMode>());

            var id = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = id is null ? null : await c.Users.FindByIdAsync(id);
            if (user is null)
                return (MenuViewer.Anonymous, (IReadOnlyDictionary<Guid, OverrideMode>)new Dictionary<Guid, OverrideMode>());

            var role = EffectiveRole(await c.Users.GetRolesAsync(user));
            return (new MenuViewer(true, role), await c.Site.GetOverridesAsync(user.Id));
        });

    /// <summary>The viewer for "preview as": any person on the list.</summary>
    public Task<(MenuViewer Viewer, IReadOnlyDictionary<Guid, OverrideMode> Overrides)?> GetViewerByIdAsync(ClaimsPrincipal principal, string userId)
        => RunAsync<(MenuViewer, IReadOnlyDictionary<Guid, OverrideMode>)?>(async c =>
        {
            await RequireAsync(c, principal, AdminPermissions.Menus);
            var user = await c.Users.FindByIdAsync(userId);
            if (user is null) return null;
            var role = EffectiveRole(await c.Users.GetRolesAsync(user));
            return (new MenuViewer(true, role), await c.Site.GetOverridesAsync(user.Id));
        });

    // ---------------------------------------------------------------- roles

    public Task<IReadOnlyList<RoleRow>> ListRolesAsync(ClaimsPrincipal principal)
        => RunAsync<IReadOnlyList<RoleRow>>(async c =>
        {
            var actor = await ActorAsync(c, principal);
            if (actor is null || !actor.AnyPermission) throw new AdminException("You don't have permission to do that.");

            var rows = new List<RoleRow>();
            foreach (var role in await c.Roles.Roles.AsNoTracking().OrderBy(r => r.Name).ToListAsync())
            {
                var claims = await c.Roles.GetClaimsAsync(role);
                var perms = claims.Where(x => x.Type == AdminPermissions.ClaimType && AdminPermissions.IsKnown(x.Value)).Select(x => x.Value).ToHashSet();
                var desc = claims.FirstOrDefault(x => x.Type == DescriptionClaim)?.Value ?? string.Empty;
                var members = string.Equals(role.Name, BuiltInRoles.Learner, StringComparison.OrdinalIgnoreCase)
                    ? await LearnerCountAsync(c)
                    : (await c.Users.GetUsersInRoleAsync(role.Name!)).Count;
                rows.Add(new RoleRow(role.Name!, desc, BuiltInRoles.IsBuiltIn(role.Name), perms, members));
            }

            // Built-ins first, in the order people think of them.
            return rows
                .OrderBy(r => r.Name == BuiltInRoles.Admin ? 0 : r.Name == BuiltInRoles.Learner ? 1 : 2)
                .ThenBy(r => r.Name)
                .ToList();
        });

    private static async Task<int> LearnerCountAsync(Ctx c)
    {
        var real = await c.Db.Users.CountAsync(u => u.UserName == null || !u.UserName.StartsWith(GuestAccount.Prefix));
        var withRole = await c.Db.UserRoles.Select(ur => ur.UserId).Distinct().CountAsync();
        return Math.Max(0, real - withRole);
    }

    public Task CreateRoleAsync(ClaimsPrincipal principal, string? name, string? startFrom)
        => RunAsync(async c =>
        {
            var actor = await RequireAsync(c, principal, AdminPermissions.Roles);

            name = (name ?? string.Empty).Trim();
            if (name.Length == 0) throw new AdminException("Give the role a name, for example Teacher.");
            if (name.Length > 40) throw new AdminException("Role names can be at most 40 characters.");
            if (name.Contains(';')) throw new AdminException("Role names can't contain a semicolon.");
            if (await c.Roles.FindByNameAsync(name) is not null) throw new AdminException($"A role called {name} already exists.");

            var created = await c.Roles.CreateAsync(new IdentityRole(name));
            if (!created.Succeeded) throw new AdminException(string.Join(" ", created.Errors.Select(e => e.Description)));

            // Start from another role's permissions, but never beyond what the creator holds.
            var role = (await c.Roles.FindByNameAsync(name))!;
            if (!string.IsNullOrWhiteSpace(startFrom) && await c.Roles.FindByNameAsync(startFrom) is { } source)
            {
                foreach (var claim in await c.Roles.GetClaimsAsync(source))
                {
                    if (claim.Type == AdminPermissions.ClaimType && actor.Has(claim.Value))
                        await c.Roles.AddClaimAsync(role, new Claim(AdminPermissions.ClaimType, claim.Value));
                }
            }

            logger.LogInformation("{Actor} created role {Role}.", actor.Email, name);
            return true;
        });

    public Task SetPermissionAsync(ClaimsPrincipal principal, string roleName, string permission, bool on)
        => RunAsync(async c =>
        {
            var actor = await RequireAsync(c, principal, AdminPermissions.Roles);
            if (!AdminPermissions.IsKnown(permission)) throw new AdminException("Unknown permission.");
            var role = await c.Roles.FindByNameAsync(roleName) ?? throw new AdminException("That role no longer exists.");

            var before = (await PermissionsForAsync(c, [role.Name!]));
            var after = new HashSet<string>(before);
            if (on) after.Add(permission); else after.Remove(permission);

            var refusal = AccountRules.CheckEditPermissions(role.Name!, before, after, actor.Permissions);
            if (refusal is not null) throw new AdminException(refusal);

            var claim = new Claim(AdminPermissions.ClaimType, permission);
            if (on) await c.Roles.AddClaimAsync(role, claim); else await c.Roles.RemoveClaimAsync(role, claim);
            return true;
        });

    public Task SetDescriptionAsync(ClaimsPrincipal principal, string roleName, string? description)
        => RunAsync(async c =>
        {
            await RequireAsync(c, principal, AdminPermissions.Roles);
            var role = await c.Roles.FindByNameAsync(roleName) ?? throw new AdminException("That role no longer exists.");
            if (BuiltInRoles.IsBuiltIn(role.Name)) throw new AdminException($"\"{role.Name}\" is built in and can't be edited.");

            foreach (var old in (await c.Roles.GetClaimsAsync(role)).Where(x => x.Type == DescriptionClaim))
                await c.Roles.RemoveClaimAsync(role, old);

            var text = (description ?? string.Empty).Trim();
            if (text.Length > 200) text = text[..200];
            if (text.Length > 0) await c.Roles.AddClaimAsync(role, new Claim(DescriptionClaim, text));
            return true;
        });

    public Task RenameRoleAsync(ClaimsPrincipal principal, string roleName, string? newName)
        => RunAsync(async c =>
        {
            await RequireAsync(c, principal, AdminPermissions.Roles);
            var refusal = AccountRules.CheckRenameRole(roleName);
            if (refusal is not null) throw new AdminException(refusal);

            newName = (newName ?? string.Empty).Trim();
            if (newName.Length == 0) throw new AdminException("A role needs a name.");
            if (newName.Length > 40 || newName.Contains(';')) throw new AdminException("That role name isn't allowed.");
            if (string.Equals(newName, roleName, StringComparison.Ordinal)) return true;
            if (await c.Roles.FindByNameAsync(newName) is not null) throw new AdminException($"A role called {newName} already exists.");

            var role = await c.Roles.FindByNameAsync(roleName) ?? throw new AdminException("That role no longer exists.");
            role.Name = newName;
            var updated = await c.Roles.UpdateAsync(role);
            if (!updated.Succeeded) throw new AdminException(string.Join(" ", updated.Errors.Select(e => e.Description)));

            await c.Roles.UpdateNormalizedRoleNameAsync(role);
            await c.Site.RenameRoleAsync(roleName, newName);
            return true;
        });

    public Task DeleteRoleAsync(ClaimsPrincipal principal, string roleName)
        => RunAsync(async c =>
        {
            var actor = await RequireAsync(c, principal, AdminPermissions.Roles);
            var role = await c.Roles.FindByNameAsync(roleName) ?? throw new AdminException("That role no longer exists.");

            var members = (await c.Users.GetUsersInRoleAsync(role.Name!)).Count;
            var refusal = AccountRules.CheckDeleteRole(role.Name!, members);
            if (refusal is not null) throw new AdminException(refusal);

            await c.Roles.DeleteAsync(role);
            await c.Site.RenameRoleAsync(roleName, null);
            logger.LogInformation("{Actor} deleted role {Role}.", actor.Email, roleName);
            return true;
        });

    private static async Task<string?> ResolveRoleAsync(Ctx c, string? roleName)
    {
        if (string.IsNullOrWhiteSpace(roleName) || string.Equals(roleName, BuiltInRoles.Learner, StringComparison.OrdinalIgnoreCase))
            return null;

        var role = await c.Roles.FindByNameAsync(roleName) ?? throw new AdminException("That role doesn't exist.");
        return role.Name;
    }

    // ---------------------------------------------------------------- site settings and menus (the draft)

    /// <summary>The published settings and menus, for the editor to start a draft from.</summary>
    public Task<(SiteSettingsDto Settings, IReadOnlyList<MenuDto> Menus)> LoadSiteAsync(ClaimsPrincipal principal)
        => RunAsync(async c =>
        {
            await RequireAsync(c, principal, AdminPermissions.Menus);
            return (await c.Site.GetSettingsAsync(), await c.Site.ListMenusAsync());
        });

    /// <summary>Publishes the draft: menus first (so the landing page can be checked against them), then settings.</summary>
    public async Task PublishSiteAsync(ClaimsPrincipal principal, SiteSettingsDto settings, IReadOnlyList<MenuDto> menus)
    {
        await RunAsync(async c =>
        {
            var actor = await RequireAsync(c, principal, AdminPermissions.Menus);

            try
            {
                await c.Site.SaveMenusAsync(menus);
                await c.Site.SaveSettingsAsync(settings);
            }
            catch (GenericLearningApp.Domain.DomainException ex)
            {
                throw new AdminException(ex.Message);
            }

            logger.LogInformation("{Actor} published the site settings and menus.", actor.Email);
            return true;
        });

        // Every open page and the request guard now see the new configuration.
        await store.RefreshAsync();
    }

    /// <summary>Numbers for the sidebar. People stays 0 for someone who can't open that page.</summary>
    public Task<(int People, int Roles)> GetCountsAsync(ClaimsPrincipal principal)
        => RunAsync(async c =>
        {
            var actor = await ActorAsync(c, principal);
            if (actor is null || !actor.AnyPermission) return (0, 0);

            var people = actor.Has(AdminPermissions.People)
                ? await c.Db.Users.CountAsync(u => u.UserName == null || !u.UserName.StartsWith(GuestAccount.Prefix))
                : 0;
            return (people, await c.Roles.Roles.CountAsync());
        });

    // ---------------------------------------------------------------- config export

    public Task<string> GetConfigJsonAsync(ClaimsPrincipal principal)
        => RunAsync(async c =>
        {
            await RequireAsync(c, principal, AdminPermissions.Menus);

            var settings = await c.Site.GetSettingsAsync();
            var menus = await c.Site.ListMenusAsync();
            var roles = new List<object>();
            foreach (var role in await c.Roles.Roles.AsNoTracking().OrderBy(r => r.Name).ToListAsync())
            {
                var claims = await c.Roles.GetClaimsAsync(role);
                roles.Add(new
                {
                    name = role.Name,
                    description = claims.FirstOrDefault(x => x.Type == DescriptionClaim)?.Value ?? "",
                    builtIn = BuiltInRoles.IsBuiltIn(role.Name),
                    permissions = claims.Where(x => x.Type == AdminPermissions.ClaimType).Select(x => x.Value).OrderBy(x => x).ToArray(),
                });
            }

            return JsonSerializer.Serialize(new
            {
                site = new { settings.Name, settings.Tagline, settings.RequireSignIn, settings.AllowGuests, settings.AllowSignUp, settings.LandingMenuId },
                roles,
                menus = menus.Select(m => new
                {
                    m.Id, m.Name, page = m.Page.ToString(), visible = m.IsVisible, access = m.Access.ToString(),
                    roles = m.Access == MenuAccess.Roles ? m.Roles : null, url = m.Url,
                }),
            }, new JsonSerializerOptions { WriteIndented = true, DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull });
        });

    // ---------------------------------------------------------------- startup

    /// <summary>
    /// Creates the built-in roles and makes sure the configured super admin exists and is an admin.
    /// Runs on every start, so it also repairs an Admin role whose permissions were tampered with.
    /// </summary>
    public Task SeedAsync(string? superAdminEmail, string? superAdminPassword)
        => RunAsync(async c =>
        {
            await EnsureRoleAsync(c, BuiltInRoles.Admin, "Manages everything. This can't be changed.");
            await EnsureRoleAsync(c, BuiltInRoles.Learner, "Uses the site and saves their own progress.");

            // Admin always holds every permission.
            var admin = (await c.Roles.FindByNameAsync(BuiltInRoles.Admin))!;
            var have = (await c.Roles.GetClaimsAsync(admin)).Where(x => x.Type == AdminPermissions.ClaimType).Select(x => x.Value).ToHashSet();
            foreach (var p in AdminPermissions.All.Where(p => !have.Contains(p.Key)))
                await c.Roles.AddClaimAsync(admin, new Claim(AdminPermissions.ClaimType, p.Key));

            if (string.IsNullOrWhiteSpace(superAdminEmail))
            {
                logger.LogWarning("Admin:SuperAdminEmail is not set, so there is no super admin and the admin area can't be opened.");
                return true;
            }

            var email = superAdminEmail.Trim();
            var user = await c.Users.FindByEmailAsync(email);
            if (user is null)
            {
                if (string.IsNullOrEmpty(superAdminPassword))
                {
                    logger.LogWarning(
                        "Super admin {Email} has no account yet. Set Admin:SuperAdminPassword to create it, or register it and restart.", email);
                    return true;
                }

                user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true, IsSuperAdmin = true };
                var created = await c.Users.CreateAsync(user, superAdminPassword);
                if (!created.Succeeded)
                {
                    logger.LogError("Couldn't create the super admin: {Errors}", string.Join(" ", created.Errors.Select(e => e.Description)));
                    return true;
                }
                logger.LogInformation("Created the super admin account for {Email}.", email);
            }
            else if (!user.IsSuperAdmin)
            {
                user.IsSuperAdmin = true;
                await c.Users.UpdateAsync(user);
                logger.LogInformation("{Email} is now the super admin.", email);
            }

            if (!await c.Users.IsInRoleAsync(user, BuiltInRoles.Admin))
                await c.Users.AddToRoleAsync(user, BuiltInRoles.Admin);

            return true;
        });

    private static async Task EnsureRoleAsync(Ctx c, string name, string description)
    {
        var role = await c.Roles.FindByNameAsync(name);
        if (role is null)
        {
            await c.Roles.CreateAsync(new IdentityRole(name));
            role = (await c.Roles.FindByNameAsync(name))!;
        }

        if (!(await c.Roles.GetClaimsAsync(role)).Any(x => x.Type == DescriptionClaim))
            await c.Roles.AddClaimAsync(role, new Claim(DescriptionClaim, description));
    }

    private static bool IsEmail(string value)
    {
        if (value.Length is 0 or > 254) return false;
        try { return new MailAddress(value).Address == value && value.Contains('.', StringComparison.Ordinal) && value.IndexOf('@') > 0; }
        catch (FormatException) { return false; }
    }
}
