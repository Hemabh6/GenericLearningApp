namespace GenericLearningApp.Web.Data;

/// <summary>
/// "Try it as a guest" signs the visitor in as a real Identity user with no email or password,
/// so every subject they create is saved to the database like anyone else's. What makes them a
/// guest is only the user-name prefix; the browser cookie is the only way back into that account.
/// </summary>
public static class GuestAccount
{
    public const string Prefix = "guest-";

    public static string NewUserName() => Prefix + Guid.NewGuid().ToString("N");

    public static bool IsGuest(string? userName) =>
        userName is not null && userName.StartsWith(Prefix, StringComparison.Ordinal);
}
