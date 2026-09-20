namespace GenericLearningApp.Domain.Entities;

/// <summary>A single workspace: the tenant boundary every other aggregate hangs off.</summary>
public class Subject
{
    public const int NameMaxLength = 120;

    private Subject() { }

    public Subject(string userId, string name)
    {
        UserId = DomainException.RequireText(userId, "User", 450);
        Name = DomainException.RequireText(name, "Subject name", NameMaxLength);
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public string UserId { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }

    public void Rename(string name) => Name = DomainException.RequireText(name, "Subject name", NameMaxLength);
}
