using GenericLearningApp.Application.Site;
using GenericLearningApp.Domain;
using GenericLearningApp.Domain.Enums;
using GenericLearningApp.Infrastructure.Site;
using GenericLearningApp.Infrastructure.Subjects;

namespace GenericLearningApp.Application.Tests;

public class SiteConfigServiceTests : IDisposable
{
    private readonly SqliteFixture _db = new();
    private readonly ISiteConfigService _service;

    public SiteConfigServiceTests() => _service = new SiteConfigService(_db);

    public void Dispose() => _db.Dispose();

    private static MenuDto Menu(string name, MenuPage page = MenuPage.Faq, MenuAccess access = MenuAccess.Everyone,
                                bool visible = true, string[]? roles = null, string? url = null, Guid? id = null)
        => new(id ?? Guid.NewGuid(), name, page, access, roles ?? [], visible, url);

    [Fact]
    public async Task A_fresh_database_gets_default_settings_and_menus()
    {
        await _service.EnsureDefaultsAsync();

        var menus = await _service.ListMenusAsync();
        var settings = await _service.GetSettingsAsync();

        Assert.Equal(["Home", "Subjects", "FAQ"], menus.Select(m => m.Name));
        Assert.Equal(MenuAccess.Signed, menus.Single(m => m.Page == MenuPage.Subjects).Access);
        Assert.Equal("Learning Desk", settings.Name);
        Assert.True(settings.AllowGuests);
        Assert.True(settings.AllowSignUp);
        Assert.False(settings.RequireSignIn);
    }

    [Fact]
    public async Task Ensuring_defaults_twice_changes_nothing()
    {
        await _service.EnsureDefaultsAsync();
        await _service.SaveSettingsAsync((await _service.GetSettingsAsync()) with { Name = "My Desk" });

        await _service.EnsureDefaultsAsync();

        Assert.Equal("My Desk", (await _service.GetSettingsAsync()).Name);
        Assert.Equal(3, (await _service.ListMenusAsync()).Count);
    }

    [Fact]
    public async Task Settings_round_trip()
    {
        await _service.EnsureDefaultsAsync();

        await _service.SaveSettingsAsync(new SiteSettingsDto("Maths Hub", "Tagline here", true, false, false, null));

        var saved = await _service.GetSettingsAsync();
        Assert.Equal("Maths Hub", saved.Name);
        Assert.Equal("Tagline here", saved.Tagline);
        Assert.True(saved.RequireSignIn);
        Assert.False(saved.AllowGuests);
        Assert.False(saved.AllowSignUp);
    }

    [Fact]
    public async Task A_blank_site_name_is_refused()
    {
        await _service.EnsureDefaultsAsync();

        await Assert.ThrowsAsync<DomainException>(
            () => _service.SaveSettingsAsync(new SiteSettingsDto("  ", "", false, true, true, null)));
    }

    [Fact]
    public async Task Saving_menus_adds_updates_removes_and_keeps_the_given_order()
    {
        await _service.EnsureDefaultsAsync();
        var existing = await _service.ListMenusAsync();
        var home = existing.Single(m => m.Page == MenuPage.Home);

        var added = Menu("Course book", MenuPage.Link, MenuAccess.Signed, url: "https://example.com/book");
        var renamedHome = home with { Name = "Start" };

        // FAQ and Subjects are left out (removed); the new menu goes first.
        await _service.SaveMenusAsync([added, renamedHome]);

        var saved = await _service.ListMenusAsync();
        Assert.Equal(["Course book", "Start"], saved.Select(m => m.Name));
        Assert.Equal(home.Id, saved[1].Id);
        Assert.Equal("https://example.com/book", saved[0].Url);
    }

    [Fact]
    public async Task Role_lists_are_stored_and_read_back()
    {
        await _service.EnsureDefaultsAsync();

        await _service.SaveMenusAsync([Menu("Results", access: MenuAccess.Roles, roles: ["Teacher", "Class teacher"])]);

        var menu = Assert.Single(await _service.ListMenusAsync());
        Assert.Equal(["Teacher", "Class teacher"], menu.Roles);
    }

    [Fact]
    public async Task At_least_one_menu_must_be_shown()
        => await Assert.ThrowsAsync<DomainException>(() => _service.SaveMenusAsync([Menu("Hidden", visible: false)]));

    [Fact]
    public async Task Menu_names_must_be_unique_ignoring_case()
        => await Assert.ThrowsAsync<DomainException>(() => _service.SaveMenusAsync([Menu("FAQ"), Menu("faq")]));

    [Fact]
    public async Task A_menu_needs_a_name()
        => await Assert.ThrowsAsync<DomainException>(() => _service.SaveMenusAsync([Menu("   ")]));

    [Theory]
    [InlineData("")]
    [InlineData("not a url")]
    [InlineData("ftp://example.com")]
    [InlineData("javascript:alert(1)")]
    public async Task An_external_link_must_be_http_or_https(string url)
        => await Assert.ThrowsAsync<DomainException>(() => _service.SaveMenusAsync([Menu("Link", MenuPage.Link, url: url)]));

    [Fact]
    public async Task A_failed_save_leaves_the_published_menus_untouched()
    {
        await _service.EnsureDefaultsAsync();
        var before = await _service.ListMenusAsync();

        await Assert.ThrowsAsync<DomainException>(() => _service.SaveMenusAsync([Menu("Ok"), Menu("Bad", MenuPage.Link, url: "nope")]));

        Assert.Equal(before.Select(m => m.Name), (await _service.ListMenusAsync()).Select(m => m.Name));
    }

    [Fact]
    public async Task The_landing_page_is_cleared_when_its_menu_is_deleted()
    {
        await _service.EnsureDefaultsAsync();
        var faq = (await _service.ListMenusAsync()).Single(m => m.Page == MenuPage.Faq);
        await _service.SaveSettingsAsync((await _service.GetSettingsAsync()) with { LandingMenuId = faq.Id });
        Assert.Equal(faq.Id, (await _service.GetSettingsAsync()).LandingMenuId);

        await _service.SaveMenusAsync((await _service.ListMenusAsync()).Where(m => m.Id != faq.Id).ToList());

        Assert.Null((await _service.GetSettingsAsync()).LandingMenuId);
    }

    [Fact]
    public async Task A_landing_page_that_is_hidden_or_unknown_is_ignored()
    {
        await _service.EnsureDefaultsAsync();

        await _service.SaveSettingsAsync((await _service.GetSettingsAsync()) with { LandingMenuId = Guid.NewGuid() });

        Assert.Null((await _service.GetSettingsAsync()).LandingMenuId);
    }

    [Fact]
    public async Task Overrides_are_set_changed_and_cleared_per_person()
    {
        await _service.EnsureDefaultsAsync();
        var faq = (await _service.ListMenusAsync()).Single(m => m.Page == MenuPage.Faq);

        await _service.SetOverrideAsync("u1", faq.Id, OverrideMode.Block);
        Assert.Equal(OverrideMode.Block, (await _service.GetOverridesAsync("u1"))[faq.Id]);

        await _service.SetOverrideAsync("u1", faq.Id, OverrideMode.Allow);
        Assert.Equal(OverrideMode.Allow, (await _service.GetOverridesAsync("u1"))[faq.Id]);

        await _service.SetOverrideAsync("u1", faq.Id, null);
        Assert.Empty(await _service.GetOverridesAsync("u1"));
    }

    [Fact]
    public async Task Overrides_belong_to_one_person()
    {
        await _service.EnsureDefaultsAsync();
        var faq = (await _service.ListMenusAsync()).Single(m => m.Page == MenuPage.Faq);

        await _service.SetOverrideAsync("u1", faq.Id, OverrideMode.Block);

        Assert.Empty(await _service.GetOverridesAsync("u2"));
        Assert.Equal(1, (await _service.CountOverridesByUserAsync())["u1"]);
    }

    [Fact]
    public async Task Deleting_a_menu_deletes_its_overrides()
    {
        await _service.EnsureDefaultsAsync();
        var menus = await _service.ListMenusAsync();
        var faq = menus.Single(m => m.Page == MenuPage.Faq);
        await _service.SetOverrideAsync("u1", faq.Id, OverrideMode.Block);

        await _service.SaveMenusAsync(menus.Where(m => m.Id != faq.Id).ToList());

        Assert.Empty(await _service.GetOverridesAsync("u1"));
    }

    [Fact]
    public async Task Removing_a_user_removes_their_overrides_and_their_subjects_only()
    {
        await _service.EnsureDefaultsAsync();
        var faq = (await _service.ListMenusAsync()).Single(m => m.Page == MenuPage.Faq);
        var subjects = new SubjectService(_db);
        await subjects.CreateAsync("gone", "Gone subject");
        await subjects.CreateAsync("stays", "Kept subject");
        await _service.SetOverrideAsync("gone", faq.Id, OverrideMode.Block);
        await _service.SetOverrideAsync("stays", faq.Id, OverrideMode.Block);

        await _service.RemoveUserAsync("gone");

        Assert.Empty(await subjects.ListAsync("gone"));
        Assert.Single(await subjects.ListAsync("stays"));
        Assert.Empty(await _service.GetOverridesAsync("gone"));
        Assert.Single(await _service.GetOverridesAsync("stays"));
    }

    [Fact]
    public async Task Renaming_a_role_updates_every_menu_that_lists_it()
    {
        await _service.EnsureDefaultsAsync();
        await _service.SaveMenusAsync([
            Menu("Results", access: MenuAccess.Roles, roles: ["Teacher", "Editor"]),
            Menu("Other", access: MenuAccess.Roles, roles: ["Editor"])]);

        await _service.RenameRoleAsync("Teacher", "Tutor");

        var menus = await _service.ListMenusAsync();
        Assert.Equal(["Tutor", "Editor"], menus.Single(m => m.Name == "Results").Roles);
        Assert.Equal(["Editor"], menus.Single(m => m.Name == "Other").Roles);
    }

    [Fact]
    public async Task Deleting_a_role_removes_it_from_menus_but_keeps_the_menus()
    {
        await _service.EnsureDefaultsAsync();
        await _service.SaveMenusAsync([Menu("Results", access: MenuAccess.Roles, roles: ["Teacher", "Editor"])]);

        await _service.RenameRoleAsync("Teacher", null);

        var menu = Assert.Single(await _service.ListMenusAsync());
        Assert.Equal(["Editor"], menu.Roles);
    }
}
