using GenericLearningApp.Application.Admin;

namespace GenericLearningApp.Application.Tests;

/// <summary>The rules that keep the site administrable, above all: the super admin is never lost.</summary>
public class AccountRulesTests
{
    private static readonly PersonInfo SuperAdmin = new("super", "Admin", IsSuperAdmin: true);
    private static readonly PersonInfo OtherAdmin = new("admin2", "Admin", IsSuperAdmin: false);
    private static readonly PersonInfo Teacher = new("teacher", "Teacher", IsSuperAdmin: false);
    private static readonly PersonInfo Learner = new("learner", null, IsSuperAdmin: false);

    // ---- the super admin ----

    [Fact]
    public void The_super_admin_cannot_be_removed_by_anyone()
    {
        // Even with plenty of other admins, and even by another admin.
        Assert.Equal(AccountRules.SuperAdminMessage, AccountRules.CheckRemove(SuperAdmin, actorId: "admin2", actorIsAdmin: true, adminCount: 5));
    }

    [Fact]
    public void The_super_admin_cannot_remove_themselves_either()
        => Assert.NotNull(AccountRules.CheckRemove(SuperAdmin, actorId: "super", actorIsAdmin: true, adminCount: 5));

    [Theory]
    [InlineData(null)]
    [InlineData("Learner")]
    [InlineData("Teacher")]
    [InlineData("Admin")]
    public void The_super_admins_role_cannot_be_changed_to_anything(string? newRole)
        => Assert.Equal(AccountRules.SuperAdminMessage, AccountRules.CheckChangeRole(SuperAdmin, newRole, actorIsAdmin: true, adminCount: 5));

    // ---- removing people ----

    [Fact]
    public void Nobody_can_remove_their_own_account_from_the_admin_page()
        => Assert.NotNull(AccountRules.CheckRemove(Teacher, actorId: "teacher", actorIsAdmin: false, adminCount: 2));

    [Fact]
    public void The_last_admin_cannot_be_removed()
        => Assert.Equal("Keep at least one admin.", AccountRules.CheckRemove(OtherAdmin, actorId: "someone", actorIsAdmin: true, adminCount: 1));

    [Fact]
    public void An_admin_can_be_removed_while_another_remains()
        => Assert.Null(AccountRules.CheckRemove(OtherAdmin, actorId: "super", actorIsAdmin: true, adminCount: 2));

    [Fact]
    public void Only_an_admin_can_remove_an_admin()
        => Assert.Equal("Only an admin can remove an admin.", AccountRules.CheckRemove(OtherAdmin, actorId: "hr", actorIsAdmin: false, adminCount: 3));

    [Fact]
    public void An_ordinary_person_can_be_removed()
        => Assert.Null(AccountRules.CheckRemove(Learner, actorId: "super", actorIsAdmin: true, adminCount: 1));

    // ---- changing roles ----

    [Fact]
    public void Only_an_admin_can_give_the_Admin_role()
    {
        Assert.NotNull(AccountRules.CheckChangeRole(Learner, "Admin", actorIsAdmin: false, adminCount: 1));
        Assert.Null(AccountRules.CheckChangeRole(Learner, "Admin", actorIsAdmin: true, adminCount: 1));
    }

    [Fact]
    public void Only_an_admin_can_take_the_Admin_role_away()
        => Assert.NotNull(AccountRules.CheckChangeRole(OtherAdmin, "Teacher", actorIsAdmin: false, adminCount: 3));

    [Fact]
    public void The_last_admin_cannot_be_demoted()
        => Assert.Equal("Keep at least one admin.", AccountRules.CheckChangeRole(OtherAdmin, "Teacher", actorIsAdmin: true, adminCount: 1));

    [Fact]
    public void An_admin_can_be_demoted_while_another_remains()
        => Assert.Null(AccountRules.CheckChangeRole(OtherAdmin, "Teacher", actorIsAdmin: true, adminCount: 2));

    [Fact]
    public void Ordinary_role_changes_are_allowed()
        => Assert.Null(AccountRules.CheckChangeRole(Learner, "Teacher", actorIsAdmin: false, adminCount: 1));

    // ---- roles ----

    [Theory]
    [InlineData("Admin")]
    [InlineData("Learner")]
    [InlineData("admin")]
    public void Built_in_roles_cannot_be_deleted_or_renamed(string name)
    {
        Assert.NotNull(AccountRules.CheckDeleteRole(name, memberCount: 0));
        Assert.NotNull(AccountRules.CheckRenameRole(name));
    }

    [Fact]
    public void A_role_with_people_in_it_cannot_be_deleted()
    {
        Assert.Contains("2 people", AccountRules.CheckDeleteRole("Teacher", 2));
        Assert.Contains("1 person still has", AccountRules.CheckDeleteRole("Teacher", 1));
    }

    [Fact]
    public void An_empty_custom_role_can_be_deleted_and_renamed()
    {
        Assert.Null(AccountRules.CheckDeleteRole("Teacher", 0));
        Assert.Null(AccountRules.CheckRenameRole("Teacher"));
    }

    private static IReadOnlySet<string> Set(params string[] items) => items.ToHashSet();

    [Fact]
    public void Admin_permissions_are_locked()
        => Assert.NotNull(AccountRules.CheckEditPermissions("Admin", Set("menus"), Set(), Set("menus", "people", "roles")));

    [Fact]
    public void Nobody_can_grant_a_permission_they_do_not_hold()
    {
        var refusal = AccountRules.CheckEditPermissions("Teacher", Set(), Set("people"), actorPermissions: Set("roles"));

        Assert.Equal("You can't grant a permission you don't have yourself.", refusal);
    }

    [Fact]
    public void A_permission_you_hold_can_be_granted_and_any_can_be_taken_away()
    {
        Assert.Null(AccountRules.CheckEditPermissions("Teacher", Set(), Set("menus"), Set("menus")));
        Assert.Null(AccountRules.CheckEditPermissions("Teacher", Set("people"), Set(), Set("roles")));
    }
}
