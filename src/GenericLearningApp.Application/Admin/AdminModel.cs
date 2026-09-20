namespace GenericLearningApp.Application.Admin;

/// <summary>Names of the roles the app creates itself and never lets an admin change or delete.</summary>
public static class BuiltInRoles
{
    public const string Admin = "Admin";
    public const string Learner = "Learner";

    public static bool IsBuiltIn(string? name)
        => string.Equals(name, Admin, StringComparison.OrdinalIgnoreCase)
        || string.Equals(name, Learner, StringComparison.OrdinalIgnoreCase);
}

/// <summary>One thing an admin-area role can be allowed to do. Only what the app really enforces is listed.</summary>
public record PermissionInfo(string Key, string Group, string Title, string Description);

public static class AdminPermissions
{
    /// <summary>Permissions are stored as claims of this type on the role.</summary>
    public const string ClaimType = "perm";

    public const string Menus = "menus";
    public const string People = "people";
    public const string Roles = "roles";

    public static readonly IReadOnlyList<PermissionInfo> All =
    [
        new(Menus, "Website", "Change menus and site settings",
            "Edit the site's name, sign-in rules and top menus."),
        new(People, "People", "Invite people and change their roles",
            "Invite, remove, assign roles and set per-person menu access."),
        new(Roles, "People", "Create and edit roles",
            "Add roles and change what each one can do."),
    ];

    public static bool IsKnown(string? key) => All.Any(p => p.Key == key);
}
