using GenericLearningApp.Domain.Enums;

namespace GenericLearningApp.Domain.Entities;

/// <summary>A single thing to watch or read, with the notes and tick that belong to it.</summary>
public class ContentItem
{
    public const int TitleMaxLength = 400;

    private ContentItem() { }

    public ContentItem(ContentNode? node, string title, ContentKind kind, string? url = null, int sortOrder = 0)
    {
        SubjectId = node?.SubjectId ?? Guid.Empty;
        NodeId = node?.Id;
        Node = node;
        Title = DomainException.RequireText(title, "Title", TitleMaxLength);
        Kind = kind;
        Url = NormaliseUrl(url);
        SortOrder = sortOrder;
    }

    public ContentItem(Guid subjectId, string title, ContentKind kind, string? url = null, int sortOrder = 0)
    {
        DomainException.Require(subjectId != Guid.Empty, "Item must belong to a subject.");
        SubjectId = subjectId;
        Title = DomainException.RequireText(title, "Title", TitleMaxLength);
        Kind = kind;
        Url = NormaliseUrl(url);
        SortOrder = sortOrder;
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid SubjectId { get; private set; }

    /// <summary>Null means the item sits in the Ungrouped bucket, as an import with no section does.</summary>
    public Guid? NodeId { get; private set; }
    public ContentNode? Node { get; private set; }

    public string Title { get; private set; } = string.Empty;
    public string? Url { get; private set; }
    public ContentKind Kind { get; private set; }
    public string Notes { get; private set; } = string.Empty;
    public bool IsDone { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public int SortOrder { get; private set; }

    public void Edit(string title, ContentKind kind, string? url)
    {
        Title = DomainException.RequireText(title, "Title", TitleMaxLength);
        Kind = kind;
        Url = NormaliseUrl(url);
    }

    public void WriteNotes(string? notes) => Notes = notes ?? string.Empty;

    public void MoveTo(ContentNode? node, int sortOrder)
    {
        if (node is not null)
            DomainException.Require(node.SubjectId == SubjectId, "An item cannot move to another subject.");

        NodeId = node?.Id;
        Node = node;
        SortOrder = sortOrder;
    }

    public void SetDone(bool isDone)
    {
        if (isDone == IsDone) return;
        IsDone = isDone;
        CompletedAt = isDone ? DateTimeOffset.UtcNow : null;
    }

    private static string? NormaliseUrl(string? url)
    {
        var trimmed = (url ?? string.Empty).Trim();
        if (trimmed.Length == 0) return null;
        DomainException.Require(
            Uri.TryCreate(trimmed, UriKind.Absolute, out var parsed)
                && (parsed.Scheme == Uri.UriSchemeHttp || parsed.Scheme == Uri.UriSchemeHttps),
            "A link must be an http or https URL.");
        return trimmed;
    }
}
