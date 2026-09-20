using GenericLearningApp.Application.Site;

namespace GenericLearningApp.Web.Admin;

public sealed record SiteSnapshot(SiteSettingsDto Settings, IReadOnlyList<MenuDto> Menus)
{
    public static readonly SiteSnapshot Empty = new(
        new SiteSettingsDto("Learning Desk", "", RequireSignIn: false, AllowGuests: true, AllowSignUp: true, LandingMenuId: null),
        []);
}

/// <summary>
/// The published site configuration, held in memory so every request and every open circuit can read
/// it without a query. The app runs as one instance, so a refresh here is a refresh for everyone;
/// <see cref="Changed"/> lets connected pages redraw their navigation the moment an admin publishes.
/// </summary>
public sealed class SiteConfigStore(IServiceScopeFactory scopes)
{
    private volatile SiteSnapshot _current = SiteSnapshot.Empty;

    public SiteSnapshot Current => _current;

    public event Action? Changed;

    public async Task RefreshAsync(CancellationToken ct = default)
    {
        await using var scope = scopes.CreateAsyncScope();
        var site = scope.ServiceProvider.GetRequiredService<ISiteConfigService>();

        _current = new SiteSnapshot(await site.GetSettingsAsync(ct), await site.ListMenusAsync(ct));
        Changed?.Invoke();
    }
}
