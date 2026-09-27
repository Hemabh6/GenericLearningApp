using GenericLearningApp.Application.Admin;
using GenericLearningApp.Domain.Enums;

namespace GenericLearningApp.Application.Site;

/// <summary>
/// Who can open a menu. Pure, so the navigation, the request guard and the admin's
/// "preview as" all give the same answer.
/// </summary>
public static class MenuAccessRules
{
    public static bool IsAllowed(MenuDto menu, MenuViewer viewer, OverrideMode? @override = null)
    {
        if (!viewer.IsSignedIn) return menu.Access == MenuAccess.Everyone;

        // A per-person exception beats the menu's rule.
        if (@override == OverrideMode.Allow) return true;
        if (@override == OverrideMode.Block) return false;

        return menu.Access switch
        {
            MenuAccess.Everyone or MenuAccess.Signed => true,
            MenuAccess.Roles => IsAdmin(viewer.Role)
                || menu.Roles.Contains(EffectiveRole(viewer.Role), StringComparer.OrdinalIgnoreCase),
            _ => false,
        };
    }

    /// <summary>The menus this viewer sees in the navigation, in order.</summary>
    public static IReadOnlyList<MenuDto> Visible(
        IEnumerable<MenuDto> menus, MenuViewer viewer, IReadOnlyDictionary<Guid, OverrideMode>? overrides = null)
        => menus
            .Where(m => m.IsVisible)
            .Where(m => IsAllowed(m, viewer, overrides is not null && overrides.TryGetValue(m.Id, out var o) ? o : null))
            .ToList();

    public static string EffectiveRole(string? role) => string.IsNullOrWhiteSpace(role) ? BuiltInRoles.Learner : role;

    private static bool IsAdmin(string? role) => string.Equals(role, BuiltInRoles.Admin, StringComparison.OrdinalIgnoreCase);
}
