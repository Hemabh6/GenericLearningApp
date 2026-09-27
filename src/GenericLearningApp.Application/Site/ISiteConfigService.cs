using GenericLearningApp.Domain.Enums;

namespace GenericLearningApp.Application.Site;

/// <summary>
/// The site-wide configuration an administrator edits: settings, the top menus, and the
/// per-person menu exceptions. Nothing here is per-user learning data.
/// </summary>
public interface ISiteConfigService
{
    /// <summary>Creates the settings row and the default menus if this is a fresh database.</summary>
    Task EnsureDefaultsAsync(CancellationToken ct = default);

    Task<SiteSettingsDto> GetSettingsAsync(CancellationToken ct = default);

    Task SaveSettingsAsync(SiteSettingsDto settings, CancellationToken ct = default);

    Task<IReadOnlyList<MenuDto>> ListMenusAsync(CancellationToken ct = default);

    /// <summary>
    /// Replaces the whole menu set in one transaction: menus with a known id are updated, new ids
    /// are added, and menus missing from <paramref name="menus"/> are removed. List order is menu order.
    /// </summary>
    Task SaveMenusAsync(IReadOnlyList<MenuDto> menus, CancellationToken ct = default);

    Task<IReadOnlyDictionary<Guid, OverrideMode>> GetOverridesAsync(string userId, CancellationToken ct = default);

    /// <summary>Sets one person's exception for one menu; a null mode clears it.</summary>
    Task SetOverrideAsync(string userId, Guid menuId, OverrideMode? mode, CancellationToken ct = default);

    /// <summary>How many people have at least one exception, keyed by user id.</summary>
    Task<IReadOnlyDictionary<string, int>> CountOverridesByUserAsync(CancellationToken ct = default);

    Task RemoveUserAsync(string userId, CancellationToken ct = default);

    /// <summary>Renames a role wherever a menu lists it. A null new name removes the role from every menu.</summary>
    Task RenameRoleAsync(string oldName, string? newName, CancellationToken ct = default);
}
