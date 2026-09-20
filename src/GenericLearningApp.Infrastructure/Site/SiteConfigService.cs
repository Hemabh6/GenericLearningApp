using GenericLearningApp.Application.Site;
using GenericLearningApp.Domain;
using GenericLearningApp.Domain.Entities;
using GenericLearningApp.Domain.Enums;
using GenericLearningApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GenericLearningApp.Infrastructure.Site;

/// <summary>A fresh context per call, for the same reason as <c>SubjectService</c>: callers are long-lived.</summary>
public class SiteConfigService(IDbContextFactory<LearningDbContext> factory) : ISiteConfigService
{
    public async Task EnsureDefaultsAsync(CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        if (!await db.SiteSettings.AnyAsync(ct))
        {
            db.SiteSettings.Add(SiteSettings.Default());
        }

        if (!await db.MenuItems.AnyAsync(ct))
        {
            db.MenuItems.AddRange(
                new MenuItem(Guid.NewGuid(), "Home", MenuPage.Home, MenuAccess.Everyone, null, true, 0, null),
                new MenuItem(Guid.NewGuid(), "Subjects", MenuPage.Subjects, MenuAccess.Signed, null, true, 1, null),
                new MenuItem(Guid.NewGuid(), "FAQ", MenuPage.Faq, MenuAccess.Everyone, null, true, 2, null));
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task<SiteSettingsDto> GetSettingsAsync(CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var s = await db.SiteSettings.AsNoTracking().SingleOrDefaultAsync(ct) ?? SiteSettings.Default();
        return ToDto(s);
    }

    public async Task SaveSettingsAsync(SiteSettingsDto settings, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        var row = await db.SiteSettings.SingleOrDefaultAsync(ct);
        if (row is null)
        {
            row = SiteSettings.Default();
            db.SiteSettings.Add(row);
        }

        // A landing page must be a menu that exists and is shown; otherwise fall back to Home.
        var landing = settings.LandingMenuId;
        if (landing is not null && !await db.MenuItems.AnyAsync(m => m.Id == landing && m.IsVisible, ct))
        {
            landing = null;
        }

        row.Update(settings.Name, settings.Tagline, settings.RequireSignIn, settings.AllowGuests, settings.AllowSignUp, landing);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<MenuDto>> ListMenusAsync(CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var rows = await db.MenuItems.AsNoTracking().OrderBy(m => m.Position).ToListAsync(ct);
        return rows.Select(ToDto).ToList();
    }

    public async Task SaveMenusAsync(IReadOnlyList<MenuDto> menus, CancellationToken ct = default)
    {
        DomainException.Require(menus.Any(m => m.IsVisible), "Show at least one menu.");

        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var m in menus)
        {
            var name = (m.Name ?? string.Empty).Trim();
            DomainException.Require(name.Length > 0, "Every menu needs a name.");
            DomainException.Require(names.Add(name), $"Two menus are both called \"{name}\".");
        }

        await using var db = await factory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var existing = await db.MenuItems.ToDictionaryAsync(m => m.Id, ct);

        for (var i = 0; i < menus.Count; i++)
        {
            var m = menus[i];
            if (existing.Remove(m.Id, out var row))
            {
                row.Apply(m.Name, m.Page, m.Access, m.Roles, m.IsVisible, i, m.Url);
            }
            else
            {
                db.MenuItems.Add(new MenuItem(m.Id, m.Name, m.Page, m.Access, m.Roles, m.IsVisible, i, m.Url));
            }
        }

        // Whatever is left was removed in the editor; its per-person exceptions go with it.
        db.MenuItems.RemoveRange(existing.Values);
        await db.SaveChangesAsync(ct);

        // The landing page can't point at a menu that is gone or hidden.
        var settings = await db.SiteSettings.SingleOrDefaultAsync(ct);
        if (settings?.LandingMenuId is { } landing
            && !await db.MenuItems.AnyAsync(m => m.Id == landing && m.IsVisible, ct))
        {
            settings.Update(settings.Name, settings.Tagline, settings.RequireSignIn, settings.AllowGuests, settings.AllowSignUp, null);
            await db.SaveChangesAsync(ct);
        }

        await tx.CommitAsync(ct);
    }

    public async Task<IReadOnlyDictionary<Guid, OverrideMode>> GetOverridesAsync(string userId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.MenuOverrides.AsNoTracking()
            .Where(o => o.UserId == userId)
            .ToDictionaryAsync(o => o.MenuItemId, o => o.Mode, ct);
    }

    public async Task SetOverrideAsync(string userId, Guid menuId, OverrideMode? mode, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        var row = await db.MenuOverrides.SingleOrDefaultAsync(o => o.UserId == userId && o.MenuItemId == menuId, ct);
        if (mode is null)
        {
            if (row is not null) db.MenuOverrides.Remove(row);
        }
        else if (row is null)
        {
            db.MenuOverrides.Add(new MenuOverride(menuId, userId, mode.Value));
        }
        else
        {
            row.Set(mode.Value);
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyDictionary<string, int>> CountOverridesByUserAsync(CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.MenuOverrides.AsNoTracking()
            .GroupBy(o => o.UserId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.UserId, g => g.Count, ct);
    }

    public async Task RemoveUserAsync(string userId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        await db.MenuOverrides.Where(o => o.UserId == userId).ExecuteDeleteAsync(ct);

        // Their subjects and everything under them go too; the FKs cascade.
        await db.Subjects.Where(s => s.UserId == userId).ExecuteDeleteAsync(ct);
    }

    public async Task RenameRoleAsync(string oldName, string? newName, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        var menus = await db.MenuItems.ToListAsync(ct);
        foreach (var m in menus.Where(m => m.Roles.Contains(oldName, StringComparer.OrdinalIgnoreCase)))
        {
            var roles = m.Roles
                .Select(r => string.Equals(r, oldName, StringComparison.OrdinalIgnoreCase) ? newName : r)
                .Where(r => !string.IsNullOrWhiteSpace(r))
                .Cast<string>();

            m.Apply(m.Name, m.Page, m.Access, roles, m.IsVisible, m.Position, m.Url);
        }

        await db.SaveChangesAsync(ct);
    }

    private static SiteSettingsDto ToDto(SiteSettings s)
        => new(s.Name, s.Tagline, s.RequireSignIn, s.AllowGuests, s.AllowSignUp, s.LandingMenuId);

    private static MenuDto ToDto(MenuItem m)
        => new(m.Id, m.Name, m.Page, m.Access, m.Roles, m.IsVisible, m.Url);
}
