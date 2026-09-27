namespace GenericLearningApp.Domain.Entities;

/// <summary>A multiple-choice question. Options are stored as jsonb: always a short list.</summary>
public class Question
{
    public const int MinOptions = 2;
    public const int MaxOptions = 10;

    private Question() { }

    internal Question(
        QuestionSet set, string text, IReadOnlyList<string> options, int correctIndex, string? explanation, int sortOrder)
    {
        var cleaned = Clean(options);
        DomainException.Require(
            correctIndex >= 0 && correctIndex < cleaned.Count,
            "The correct answer must be one of the options.");

        QuestionSetId = set.Id;
        QuestionSet = set;
        Text = DomainException.RequireText(text, "Question", 1000);
        Options = cleaned;
        CorrectIndex = correctIndex;
        Explanation = (explanation ?? string.Empty).Trim();
        SortOrder = sortOrder;
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid QuestionSetId { get; private set; }
    public QuestionSet? QuestionSet { get; private set; }
    public string Text { get; private set; } = string.Empty;
    public IReadOnlyList<string> Options { get; private set; } = [];

    /// <summary>Never sent to the browser before an attempt is submitted.</summary>
    public int CorrectIndex { get; private set; }

    public string Explanation { get; private set; } = string.Empty;
    public int SortOrder { get; private set; }

    public bool IsCorrect(int chosenIndex) => chosenIndex == CorrectIndex;

    public void Edit(string text, IReadOnlyList<string> options, int correctIndex, string? explanation)
    {
        var cleaned = Clean(options);
        DomainException.Require(
            correctIndex >= 0 && correctIndex < cleaned.Count,
            "The correct answer must be one of the options.");

        Text = DomainException.RequireText(text, "Question", 1000);
        Options = cleaned;
        CorrectIndex = correctIndex;
        Explanation = (explanation ?? string.Empty).Trim();
    }

    private static List<string> Clean(IReadOnlyList<string> options)
    {
        var cleaned = (options ?? [])
            .Select(o => (o ?? string.Empty).Trim())
            .Where(o => o.Length > 0)
            .ToList();

        DomainException.Require(
            cleaned.Count is >= MinOptions and <= MaxOptions,
            $"A question needs between {MinOptions} and {MaxOptions} options.");
        DomainException.Require(
            cleaned.Distinct(StringComparer.OrdinalIgnoreCase).Count() == cleaned.Count,
            "Options must be distinct.");
        return cleaned;
    }
}
