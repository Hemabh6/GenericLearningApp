using GenericLearningApp.Domain.Enums;

namespace GenericLearningApp.Domain.Entities;

/// <summary>Lets one person open (or not open) one menu regardless of the menu's normal rule.</summary>
public class MenuOverride
{
    private MenuOverride() { }

    public MenuOverride(Guid menuItemId, string userId, OverrideMode mode)
    {
        DomainException.Require(menuItemId != Guid.Empty, "Override needs a menu.");
        MenuItemId = menuItemId;
        UserId = DomainException.RequireText(userId, "User", 450);
        Mode = mode;
    }

    public Guid MenuItemId { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    public OverrideMode Mode { get; private set; }

    public void Set(OverrideMode mode) => Mode = mode;
}
