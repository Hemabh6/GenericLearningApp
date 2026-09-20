namespace GenericLearningApp.Domain.Entities;

/// <summary>A test: a named set of questions a subject can be examined on more than once.</summary>
public class QuestionSet
{
    private readonly List<Question> _questions = [];
    private readonly List<TestAttempt> _attempts = [];

    private QuestionSet() { }

    public QuestionSet(Guid subjectId, string name)
    {
        DomainException.Require(subjectId != Guid.Empty, "Question set must belong to a subject.");
        SubjectId = subjectId;
        Name = DomainException.RequireText(name, "Test name", 200);
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid SubjectId { get; private set; }
    public Subject? Subject { get; private set; }
    public string Name { get; private set; } = string.Empty;

    public IReadOnlyList<Question> Questions => _questions;
    public IReadOnlyCollection<TestAttempt> Attempts => _attempts;

    public void Rename(string name) => Name = DomainException.RequireText(name, "Test name", 200);

    public Question AddQuestion(string text, IReadOnlyList<string> options, int correctIndex, string? explanation = null)
    {
        var question = new Question(this, text, options, correctIndex, explanation, _questions.Count);
        _questions.Add(question);
        return question;
    }

    /// <summary>
    /// Scores an attempt server-side. <paramref name="answers"/> maps question id to the chosen
    /// option index; a question left out counts as wrong.
    /// </summary>
    public TestAttempt Submit(string userId, IReadOnlyDictionary<Guid, int> answers)
    {
        DomainException.Require(_questions.Count > 0, "A test with no questions cannot be submitted.");

        var score = _questions.Count(q => answers.TryGetValue(q.Id, out var chosen) && q.IsCorrect(chosen));
        var attempt = new TestAttempt(this, userId, score, _questions.Count, answers);
        _attempts.Add(attempt);
        return attempt;
    }
}
