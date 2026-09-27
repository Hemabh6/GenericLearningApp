namespace GenericLearningApp.Domain.Entities;

/// <summary>
/// One level of the content tree. Depth is unbounded: "Math", "Math > Linear algebra",
/// "Math > Linear algebra > Vectors" are all nodes. <see cref="MaterializedPath"/> makes a
/// subtree read one indexed range scan instead of a recursive query.
/// </summary>
public class ContentNode
{
    public const int NameMaxLength = 200;
    public const char PathSeparator = '/';

    private readonly List<ContentItem> _items = [];

    private ContentNode() { }

    public ContentNode(Guid subjectId, string name, ContentNode? parent = null, int sortOrder = 0)
    {
        DomainException.Require(subjectId != Guid.Empty, "Content node must belong to a subject.");
        SubjectId = subjectId;
        Name = DomainException.RequireText(name, "Section name", NameMaxLength);
        SortOrder = sortOrder;
        AttachTo(parent);
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid SubjectId { get; private set; }
    public Subject? Subject { get; private set; }
    public Guid? ParentId { get; private set; }
    public ContentNode? Parent { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public int SortOrder { get; private set; }

    /// <summary>Ancestor ids, slash-delimited, ending in a separator: "/a/b/".</summary>
    public string MaterializedPath { get; private set; } = PathSeparator.ToString();

    public IReadOnlyCollection<ContentItem> Items => _items;

    /// <summary>Number of ancestors above this node; a root node is depth 0.</summary>
    public int Depth => MaterializedPath.Count(c => c == PathSeparator) - 1;

    public void Rename(string name) => Name = DomainException.RequireText(name, "Section name", NameMaxLength);

    public void MoveTo(int sortOrder) => SortOrder = sortOrder;

    /// <summary>Re-parents this node, rejecting any move that would create a cycle.</summary>
    public void AttachTo(ContentNode? parent)
    {
        if (parent is not null)
        {
            DomainException.Require(parent.Id != Id, "A section cannot be its own parent.");
            DomainException.Require(parent.SubjectId == SubjectId, "A section cannot move to another subject.");
            DomainException.Require(
                !parent.MaterializedPath.Contains($"{PathSeparator}{Id}{PathSeparator}"),
                "A section cannot move beneath one of its own descendants.");
        }

        ParentId = parent?.Id;
        Parent = parent;
        MaterializedPath = parent is null
            ? PathSeparator.ToString()
            : $"{parent.MaterializedPath}{parent.Id}{PathSeparator}";
    }

    public ContentItem AddItem(string title, Enums.ContentKind kind, string? url = null)
    {
        var item = new ContentItem(this, title, kind, url, _items.Count);
        _items.Add(item);
        return item;
    }
}
