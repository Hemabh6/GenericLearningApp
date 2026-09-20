namespace GenericLearningApp.Domain.Entities;

/// <summary>One row of the quick-reference table: term, meaning, and when to use it.</summary>
public class ReferenceTerm
{
    private ReferenceTerm() { }

    public ReferenceTerm(Guid subjectId, string term, string meaning, string useItWhen, int sortOrder = 0)
    {
        DomainException.Require(subjectId != Guid.Empty, "Reference term must belong to a subject.");
        SubjectId = subjectId;
        Term = DomainException.RequireText(term, "Term", 200);
        Meaning = DomainException.RequireText(meaning, "Meaning", 1000);
        UseItWhen = (useItWhen ?? string.Empty).Trim();
        SortOrder = sortOrder;
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid SubjectId { get; private set; }
    public Subject? Subject { get; private set; }
    public string Term { get; private set; } = string.Empty;
    public string Meaning { get; private set; } = string.Empty;
    public string UseItWhen { get; private set; } = string.Empty;
    public int SortOrder { get; private set; }

    public void Edit(string term, string meaning, string useItWhen)
    {
        Term = DomainException.RequireText(term, "Term", 200);
        Meaning = DomainException.RequireText(meaning, "Meaning", 1000);
        UseItWhen = (useItWhen ?? string.Empty).Trim();
    }

    public void MoveTo(int sortOrder) => SortOrder = sortOrder;
}
