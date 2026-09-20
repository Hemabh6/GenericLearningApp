namespace GenericLearningApp.Domain.Entities;

/// <summary>The whole-site settings. There is exactly one row.</summary>
public class SiteSettings
{
    public const int NameMaxLength = 80;
    public const int TaglineMaxLength = 200;

    /// <summary>The one row's key.</summary>
    public const int SingletonId = 1;

    private SiteSettings() { }

    public static SiteSettings Default() => new()
    {
        Id = SingletonId,
        Name = "Learning Desk",
        Tagline = "Notes, a watch-and-read list, a roadmap and tests, one subject at a time.",
        RequireSignIn = false,
        AllowGuests = true,
        AllowSignUp = true,
    };

    public int Id { get; private set; } = SingletonId;
    public string Name { get; private set; } = string.Empty;
    public string Tagline { get; private set; } = string.Empty;

    /// <summary>When true a visitor must sign in (or continue as a guest) before seeing any page.</summary>
    public bool RequireSignIn { get; private set; }

    public bool AllowGuests { get; private set; }
    public bool AllowSignUp { get; private set; }

    /// <summary>The menu the site opens on. Null means the Home menu.</summary>
    public Guid? LandingMenuId { get; private set; }

    public void Update(string? name, string? tagline, bool requireSignIn, bool allowGuests, bool allowSignUp, Guid? landingMenuId)
    {
        Name = DomainException.RequireText(name, "Site name", NameMaxLength);
        Tagline = Clip(tagline, TaglineMaxLength);
        RequireSignIn = requireSignIn;
        AllowGuests = allowGuests;
        AllowSignUp = allowSignUp;
        LandingMenuId = landingMenuId;
    }

    private static string Clip(string? value, int maxLength)
    {
        var trimmed = (value ?? string.Empty).Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }
}
