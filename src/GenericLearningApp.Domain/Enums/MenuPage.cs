namespace GenericLearningApp.Domain.Enums;

/// <summary>What a top-level menu opens. Only pages the app really has are offered.</summary>
public enum MenuPage
{
    Home = 0,
    Subjects = 1,
    Faq = 2,
    Link = 3,
}

/// <summary>Who may open a menu, before any per-person override.</summary>
public enum MenuAccess
{
    Everyone = 0,
    Signed = 1,
    Roles = 2,
}

/// <summary>A per-person exception to a menu's normal access rule.</summary>
public enum OverrideMode
{
    Allow = 1,
    Block = 2,
}
