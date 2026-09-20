namespace GenericLearningApp.Domain;

/// <summary>Raised when an operation would leave an entity in an invalid state.</summary>
public sealed class DomainException(string message) : Exception(message)
{
    public static void Require(bool condition, string message)
    {
        if (!condition) throw new DomainException(message);
    }

    public static string RequireText(string? value, string field, int maxLength)
    {
        var trimmed = (value ?? string.Empty).Trim();
        Require(trimmed.Length > 0, $"{field} is required.");
        Require(trimmed.Length <= maxLength, $"{field} must be {maxLength} characters or fewer.");
        return trimmed;
    }
}
