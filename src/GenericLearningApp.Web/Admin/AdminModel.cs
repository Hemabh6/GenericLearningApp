using GenericLearningApp.Application.Admin;

namespace GenericLearningApp.Web.Admin;

/// <summary>A refusal the admin page can show as-is.</summary>
public sealed class AdminException(string message) : Exception(message);

/// <summary>Who is acting, read fresh from the database so a role change applies at once.</summary>
public sealed record Actor(string Id, string Email, string? Role, bool IsSuperAdmin, IReadOnlySet<string> Permissions)
{
    public bool IsAdmin => AccountRules.IsAdmin(Role);
    public bool Has(string permission) => Permissions.Contains(permission);
    public bool AnyPermission => Permissions.Count > 0;
}

public sealed record PersonRow(
    string Id, string Name, string Email, string? Role, string Status, bool IsSuperAdmin, bool IsYou, int Overrides);

public sealed record RoleRow(
    string Name, string Description, bool BuiltIn, IReadOnlySet<string> Permissions, int Members);

public sealed record InviteResult(string Email, string Link);
