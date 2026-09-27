namespace GenericLearningApp.Application.Admin;

/// <summary>Just what the protection rules need to know about a person.</summary>
public record PersonInfo(string Id, string? Role, bool IsSuperAdmin);

/// <summary>
/// The rules that keep the site administrable. Each returns a message when the action is refused,
/// or null when it is allowed, so the same wording reaches the admin page and the service layer.
/// The super admin is protected here rather than only in the UI: hiding a Remove button is not
/// protection, refusing the call is.
/// </summary>
public static class AccountRules
{
    public const string SuperAdminMessage = "The super admin can't be removed, and their role can't be changed.";

    /// <summary>Removing a person's account.</summary>
    public static string? CheckRemove(PersonInfo target, string actorId, bool actorIsAdmin, int adminCount)
    {
        if (target.IsSuperAdmin) return SuperAdminMessage;
        if (target.Id == actorId) return "You can't remove your own account from here.";
        if (IsAdmin(target.Role) && !actorIsAdmin) return "Only an admin can remove an admin.";
        if (IsAdmin(target.Role) && adminCount <= 1) return "Keep at least one admin.";
        return null;
    }

    /// <summary>Giving a person a different role.</summary>
    public static string? CheckChangeRole(PersonInfo target, string? newRole, bool actorIsAdmin, int adminCount)
    {
        if (target.IsSuperAdmin) return SuperAdminMessage;

        var touchesAdmin = IsAdmin(newRole) || IsAdmin(target.Role);
        if (touchesAdmin && !actorIsAdmin) return "Only an admin can give or take away the Admin role.";
        if (IsAdmin(target.Role) && !IsAdmin(newRole) && adminCount <= 1) return "Keep at least one admin.";
        return null;
    }

    public static string? CheckDeleteRole(string roleName, int memberCount)
    {
        if (BuiltInRoles.IsBuiltIn(roleName)) return $"\"{roleName}\" is built in and can't be deleted.";
        if (memberCount > 0)
            return $"{memberCount} {(memberCount == 1 ? "person still has" : "people still have")} the role \"{roleName}\". Move them to another role first.";
        return null;
    }

    public static string? CheckRenameRole(string roleName)
        => BuiltInRoles.IsBuiltIn(roleName) ? $"\"{roleName}\" is built in and can't be renamed." : null;

    /// <summary>
    /// Changing a role's permissions. Admin is locked, and nobody can hand out a permission they
    /// don't hold themselves, so editing roles can't be used to climb.
    /// </summary>
    public static string? CheckEditPermissions(
        string roleName, IReadOnlySet<string> before, IReadOnlySet<string> after, IReadOnlySet<string> actorPermissions)
    {
        if (string.Equals(roleName, BuiltInRoles.Admin, StringComparison.OrdinalIgnoreCase))
            return "Admin permissions are locked so someone can always manage the site.";

        var granted = after.Except(before).FirstOrDefault(p => !actorPermissions.Contains(p));
        return granted is null ? null : "You can't grant a permission you don't have yourself.";
    }

    public static bool IsAdmin(string? role) => string.Equals(role, BuiltInRoles.Admin, StringComparison.OrdinalIgnoreCase);
}
