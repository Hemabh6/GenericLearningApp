using GenericLearningApp.Application.Site;
using GenericLearningApp.Domain.Enums;

namespace GenericLearningApp.Application.Tests;

public class MenuAccessRulesTests
{
    private static MenuDto Menu(MenuAccess access, bool visible = true, params string[] roles)
        => new(Guid.NewGuid(), "Menu", MenuPage.Faq, access, roles, visible, null);

    private static readonly MenuViewer Learner = new(true, null);
    private static readonly MenuViewer Teacher = new(true, "Teacher");
    private static readonly MenuViewer Admin = new(true, "Admin");

    [Fact]
    public void An_anonymous_visitor_only_gets_Everyone_menus()
    {
        Assert.True(MenuAccessRules.IsAllowed(Menu(MenuAccess.Everyone), MenuViewer.Anonymous));
        Assert.False(MenuAccessRules.IsAllowed(Menu(MenuAccess.Signed), MenuViewer.Anonymous));
        Assert.False(MenuAccessRules.IsAllowed(Menu(MenuAccess.Roles, true, "Teacher"), MenuViewer.Anonymous));
    }

    [Fact]
    public void Anyone_signed_in_gets_Everyone_and_Signed_menus()
    {
        Assert.True(MenuAccessRules.IsAllowed(Menu(MenuAccess.Everyone), Learner));
        Assert.True(MenuAccessRules.IsAllowed(Menu(MenuAccess.Signed), Learner));
    }

    [Fact]
    public void A_role_menu_opens_for_its_roles_and_for_admins_only()
    {
        var menu = Menu(MenuAccess.Roles, true, "Teacher");

        Assert.True(MenuAccessRules.IsAllowed(menu, Teacher));
        Assert.True(MenuAccessRules.IsAllowed(menu, Admin));
        Assert.False(MenuAccessRules.IsAllowed(menu, Learner));
    }

    [Fact]
    public void Role_names_match_without_regard_to_case()
        => Assert.True(MenuAccessRules.IsAllowed(Menu(MenuAccess.Roles, true, "teacher"), Teacher));

    [Fact]
    public void A_role_menu_with_no_roles_is_for_admins_alone()
    {
        var menu = Menu(MenuAccess.Roles);

        Assert.True(MenuAccessRules.IsAllowed(menu, Admin));
        Assert.False(MenuAccessRules.IsAllowed(menu, Teacher));
        Assert.False(MenuAccessRules.IsAllowed(menu, Learner));
    }

    [Fact]
    public void No_role_means_Learner()
    {
        Assert.True(MenuAccessRules.IsAllowed(Menu(MenuAccess.Roles, true, "Learner"), Learner));
        Assert.Equal("Learner", MenuAccessRules.EffectiveRole(null));
        Assert.Equal("Learner", MenuAccessRules.EffectiveRole("  "));
    }

    [Fact]
    public void An_Allow_override_opens_a_menu_the_role_could_not()
        => Assert.True(MenuAccessRules.IsAllowed(Menu(MenuAccess.Roles, true, "Teacher"), Learner, OverrideMode.Allow));

    [Fact]
    public void A_Block_override_closes_a_menu_even_to_an_admin()
        => Assert.False(MenuAccessRules.IsAllowed(Menu(MenuAccess.Everyone), Admin, OverrideMode.Block));

    [Fact]
    public void An_override_cannot_open_a_menu_to_someone_who_is_not_signed_in()
        => Assert.False(MenuAccessRules.IsAllowed(Menu(MenuAccess.Signed), MenuViewer.Anonymous, OverrideMode.Allow));

    [Fact]
    public void Visible_drops_hidden_menus_and_keeps_order()
    {
        var a = Menu(MenuAccess.Everyone);
        var hidden = Menu(MenuAccess.Everyone, visible: false);
        var b = Menu(MenuAccess.Signed);

        var shown = MenuAccessRules.Visible([a, hidden, b], Learner);

        Assert.Equal([a, b], shown);
    }

    [Fact]
    public void Visible_applies_overrides_by_menu_id()
    {
        var a = Menu(MenuAccess.Everyone);
        var b = Menu(MenuAccess.Everyone);
        var overrides = new Dictionary<Guid, OverrideMode> { [b.Id] = OverrideMode.Block };

        Assert.Equal([a], MenuAccessRules.Visible([a, b], Learner, overrides));
    }

    [Fact]
    public void Href_is_base_relative_for_built_in_pages()
    {
        Assert.Equal("", (Menu(MenuAccess.Everyone) with { Page = MenuPage.Home }).Href);
        Assert.Equal("subjects", (Menu(MenuAccess.Everyone) with { Page = MenuPage.Subjects }).Href);
        Assert.Equal("faq", (Menu(MenuAccess.Everyone) with { Page = MenuPage.Faq }).Href);
        Assert.Equal("https://example.com", (Menu(MenuAccess.Everyone) with { Page = MenuPage.Link, Url = "https://example.com" }).Href);
    }
}
