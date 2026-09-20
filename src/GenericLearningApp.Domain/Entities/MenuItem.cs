using GenericLearningApp.Domain.Enums;

namespace GenericLearningApp.Domain.Entities;

/// <summary>One entry in the site's top navigation.</summary>
public class MenuItem
{
    public const int NameMaxLength = 60;
    public const int UrlMaxLength = 500;

    private MenuItem() { }

    public MenuItem(Guid id, string? name, MenuPage page, MenuAccess access, IEnumerable<string>? roles,
                    bool isVisible, int position, string? url)
    {
        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        Apply(name, page, access, roles, isVisible, position, url);
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public MenuPage Page { get; private set; }
    public MenuAccess Access { get; private set; }

    /// <summary>Role names that may open the menu when <see cref="Access"/> is Roles. Stored as a delimited column.</summary>
    public string RolesCsv { get; private set; } = string.Empty;

    public bool IsVisible { get; private set; }
    public int Position { get; private set; }
    public string? Url { get; private set; }

    public IReadOnlyList<string> Roles => RolesCsv.Length == 0
        ? []
        : RolesCsv.Split(';', StringSplitOptions.RemoveEmptyEntries);

    public void Apply(string? name, MenuPage page, MenuAccess access, IEnumerable<string>? roles,
                      bool isVisible, int position, string? url)
    {
        Name = DomainException.RequireText(name, "Menu name", NameMaxLength);
        Page = page;
        Access = access;
        RolesCsv = string.Join(';', (roles ?? []).Select(r => r.Trim()).Where(r => r.Length > 0 && !r.Contains(';')).Distinct(StringComparer.OrdinalIgnoreCase));
        IsVisible = isVisible;
        Position = position;

        if (page == MenuPage.Link)
        {
            var trimmed = (url ?? string.Empty).Trim();
            DomainException.Require(
                Uri.TryCreate(trimmed, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps),
                $"\"{Name}\" needs a link starting with http:// or https://.");
            DomainException.Require(trimmed.Length <= UrlMaxLength, $"\"{Name}\" has a link that is too long.");
            Url = trimmed;
        }
        else
        {
            Url = null;
        }
    }
}
