using System.Security.Claims;
using GenericLearningApp.Application.Site;
using GenericLearningApp.Domain.Enums;

namespace GenericLearningApp.Web.Admin;

public enum AccessDecision { Allowed, SignIn, Denied }

/// <summary>
/// Applies the admin's site rules to a path: "require sign-in", and who may open the menu behind
/// each built-in page. The request pipeline calls it for a fresh page load; the layout calls it for
/// in-circuit navigation, which never touches the pipeline.
/// </summary>
public sealed class SiteAccess(SiteConfigStore store, AdminAccountService accounts)
{
    private static readonly string[] AlwaysOpen =
        ["/account", "/_framework", "/_blazor", "/_content", "/health", "/signin-", "/error", "/not-found"];

    public static MenuPage? PageFor(string path) => Normalize(path) switch
    {
        "/" => MenuPage.Home,
        "/subjects" => MenuPage.Subjects,
        "/faq" => MenuPage.Faq,
        _ => null,
    };

    public async Task<AccessDecision> CheckAsync(ClaimsPrincipal user, string path)
    {
        var p = Normalize(path);

        // Sign-in, static files and framework endpoints must stay reachable or nobody could ever get in.
        if (Path.HasExtension(p) || AlwaysOpen.Any(prefix => p.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            return AccessDecision.Allowed;

        var snapshot = store.Current;
        var signedIn = user.Identity?.IsAuthenticated == true;

        if (snapshot.Settings.RequireSignIn && !signedIn) return AccessDecision.SignIn;

        var page = PageFor(p);
        if (page is null) return AccessDecision.Allowed;

        var menus = snapshot.Menus.Where(m => m.Page == page).ToList();
        if (menus.Count == 0) return AccessDecision.Allowed;

        var (viewer, overrides) = await accounts.GetViewerAsync(user);
        var allowed = menus.Any(m => MenuAccessRules.IsAllowed(m, viewer, overrides.TryGetValue(m.Id, out var o) ? o : null));

        if (allowed) return AccessDecision.Allowed;
        return signedIn ? AccessDecision.Denied : AccessDecision.SignIn;
    }

    private static string Normalize(string path)
    {
        var p = path.Split('?', '#')[0];
        if (!p.StartsWith('/')) p = "/" + p;
        return p.Length > 1 ? p.TrimEnd('/') : p;
    }
}
