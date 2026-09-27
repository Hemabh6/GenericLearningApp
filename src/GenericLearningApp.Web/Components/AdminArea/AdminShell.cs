using System.Security.Claims;
using System.Text.Json;
using GenericLearningApp.Application.Site;
using GenericLearningApp.Domain.Enums;
using GenericLearningApp.Web.Admin;

namespace GenericLearningApp.Web.Components.AdminArea;

public sealed class SiteEdit
{
    public string Name { get; set; } = "";
    public string Tagline { get; set; } = "";
    public bool RequireSignIn { get; set; }
    public bool AllowGuests { get; set; }
    public bool AllowSignUp { get; set; }
    public Guid? Landing { get; set; }
}

public sealed class MenuEdit
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public MenuPage Page { get; set; }
    public MenuAccess Access { get; set; }
    public List<string> Roles { get; set; } = [];
    public bool IsVisible { get; set; }
    public string Url { get; set; } = "";
}

/// <summary>
/// State shared by every admin page while the admin area is open: who is acting, the unpublished
/// draft of the site settings and menus, and the toast. The draft lives here, not in a page, so
/// moving between sections doesn't lose edits and one Publish covers all of them, as in the design.
/// </summary>
public sealed class AdminShell
{
    private sealed record Draft(SiteEdit Site, List<MenuEdit> Menus);

    private string _base = "";
    private CancellationTokenSource? _toastCts;

    public required Actor Actor { get; init; }
    public required ClaimsPrincipal User { get; init; }

    public SiteEdit Site { get; private set; } = new();
    public List<MenuEdit> Menus { get; private set; } = [];
    public bool DraftLoaded { get; private set; }

    public IReadOnlyList<RoleRow> Roles { get; set; } = [];
    public int PeopleCount { get; set; }

    public string? ToastMessage { get; private set; }
    public Func<Task>? ToastUndo { get; private set; }

    /// <summary>Raised when the bar, counts or toast need to redraw.</summary>
    public event Action? Changed;

    public void Notify() => Changed?.Invoke();

    public bool Dirty => DraftLoaded && Snapshot() != _base;

    public string Snapshot() => JsonSerializer.Serialize(new Draft(Site, Menus));

    public void LoadDraft(SiteSettingsDto settings, IEnumerable<MenuDto> menus)
    {
        Site = new SiteEdit
        {
            Name = settings.Name,
            Tagline = settings.Tagline,
            RequireSignIn = settings.RequireSignIn,
            AllowGuests = settings.AllowGuests,
            AllowSignUp = settings.AllowSignUp,
            Landing = settings.LandingMenuId,
        };
        Menus = menus.Select(m => new MenuEdit
        {
            Id = m.Id, Name = m.Name, Page = m.Page, Access = m.Access,
            Roles = m.Roles.ToList(), IsVisible = m.IsVisible, Url = m.Url ?? "",
        }).ToList();

        DraftLoaded = true;
        _base = Snapshot();
    }

    /// <summary>Puts the draft back exactly as it was at <paramref name="snapshot"/> (Undo).</summary>
    public void Restore(string snapshot)
    {
        var draft = JsonSerializer.Deserialize<Draft>(snapshot);
        if (draft is null) return;
        Site = draft.Site;
        Menus = draft.Menus;
    }

    /// <summary>
    /// A role was renamed (or deleted, with a null name). The service already fixed the published menus, so
    /// fix the draft the same way; if there were no other edits, keep the draft looking "published".
    /// </summary>
    public void RenameRoleInDraft(string oldName, string? newName)
    {
        if (!DraftLoaded) return;

        var wasDirty = Dirty;
        foreach (var menu in Menus)
        {
            menu.Roles = menu.Roles
                .Select(r => string.Equals(r, oldName, StringComparison.OrdinalIgnoreCase) ? newName : r)
                .Where(r => !string.IsNullOrWhiteSpace(r))
                .Cast<string>()
                .ToList();
        }

        if (!wasDirty) MarkPublished();
    }

    /// <summary>Forgets the edits: the draft becomes the published state again.</summary>
    public void MarkPublished() => _base = Snapshot();

    public SiteSettingsDto ToSettings()
        => new(Site.Name, Site.Tagline, Site.RequireSignIn, Site.AllowGuests, Site.AllowSignUp, Site.Landing);

    public IReadOnlyList<MenuDto> ToMenus()
        => Menus.Select(m => new MenuDto(
            m.Id, m.Name, m.Page, m.Access,
            m.Access == MenuAccess.Roles ? m.Roles.ToList() : [],
            m.IsVisible,
            m.Page == MenuPage.Link ? m.Url : null)).ToList();

    /// <summary>What would stop a publish, worded for the admin. Empty means it can go out.</summary>
    public IReadOnlyList<string> Problems()
    {
        var problems = new List<string>();

        if (string.IsNullOrWhiteSpace(Site.Name)) problems.Add("Give the site a name.");
        if (!Menus.Any(m => m.IsVisible)) problems.Add("Show at least one menu.");

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var m in Menus)
        {
            var name = (m.Name ?? "").Trim();
            if (name.Length == 0) problems.Add("Every menu needs a name.");
            else if (!seen.Add(name)) problems.Add($"Two menus are both called \"{name}\".");

            if (m.Page == MenuPage.Link && !(Uri.TryCreate(m.Url, UriKind.Absolute, out var u)
                                            && (u.Scheme == Uri.UriSchemeHttp || u.Scheme == Uri.UriSchemeHttps)))
                problems.Add($"\"{name}\" needs a link starting with http:// or https://.");
        }

        return problems.Distinct().ToList();
    }

    public void Toast(string message, Func<Task>? undo = null)
    {
        _toastCts?.Cancel();
        var cts = _toastCts = new CancellationTokenSource();
        ToastMessage = message;
        ToastUndo = undo;
        Notify();
        _ = ClearToastLaterAsync(cts.Token);
    }

    public void ClearToast()
    {
        _toastCts?.Cancel();
        ToastMessage = null;
        ToastUndo = null;
        Notify();
    }

    private async Task ClearToastLaterAsync(CancellationToken token)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(6), token);
            ToastMessage = null;
            ToastUndo = null;
            Notify();
        }
        catch (TaskCanceledException)
        {
            // A newer toast replaced this one.
        }
    }
}

/// <summary>Wording for the menu enums, shared by the pages.</summary>
public static class AdminText
{
    public static readonly IReadOnlyList<(MenuPage Value, string Label)> PageTypes =
    [
        (MenuPage.Home, "Home"),
        (MenuPage.Subjects, "Subjects"),
        (MenuPage.Faq, "FAQ"),
        (MenuPage.Link, "External link"),
    ];

    public static readonly IReadOnlyList<(MenuAccess Value, string Label)> AccessLevels =
    [
        (MenuAccess.Everyone, "Everyone"),
        (MenuAccess.Signed, "Signed-in users"),
        (MenuAccess.Roles, "Only chosen roles"),
    ];

    public static string PageName(MenuPage page) => PageTypes.First(p => p.Value == page).Label;
}
