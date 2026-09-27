namespace GenericLearningApp.Domain.Entities;

/// <summary>A free-text note. Autosaved, so writes are frequent and small.</summary>
public class Note
{
    public const int TitleMaxLength = 200;

    private Note() { }

    public Note(Guid subjectId, string? title = null, string? body = null)
    {
        DomainException.Require(subjectId != Guid.Empty, "Note must belong to a subject.");
        SubjectId = subjectId;
        Title = Clip(title, TitleMaxLength);
        Body = body ?? string.Empty;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid SubjectId { get; private set; }
    public Subject? Subject { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>Title and body may both be empty: a note is created before it is written.</summary>
    public void Edit(string? title, string? body)
    {
        Title = Clip(title, TitleMaxLength);
        Body = body ?? string.Empty;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static string Clip(string? value, int maxLength)
    {
        var trimmed = (value ?? string.Empty).Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }
}
