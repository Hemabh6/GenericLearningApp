using GenericLearningApp.Domain.Enums;

namespace GenericLearningApp.Application.Site;

public record SiteSettingsDto(
    string Name,
    string Tagline,
    bool RequireSignIn,
    bool AllowGuests,
    bool AllowSignUp,
    Guid? LandingMenuId);

/// <summary>A menu as the admin edits it and the navigation reads it.</summary>
public record MenuDto(
    Guid Id,
    string Name,
    MenuPage Page,
    MenuAccess Access,
    IReadOnlyList<string> Roles,
    bool IsVisible,
    string? Url)
{
    /// <summary>Where the menu leads. Base-relative, so it works under any path base.</summary>
    public string Href => Page switch
    {
        MenuPage.Home => "",
        MenuPage.Subjects => "subjects",
        MenuPage.Faq => "faq",
        _ => Url ?? "",
    };

    public bool IsExternal => Page == MenuPage.Link;
}

/// <summary>The signed-in state of whoever is looking at the navigation.</summary>
/// <param name="IsSignedIn">True for any account, guests included: a guest has a real account here.</param>
/// <param name="Role">The person's one role. Null or empty means the default role, Learner.</param>
public record MenuViewer(bool IsSignedIn, string? Role)
{
    public static readonly MenuViewer Anonymous = new(false, null);
}
