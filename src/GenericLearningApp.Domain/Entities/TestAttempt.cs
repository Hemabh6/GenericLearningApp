namespace GenericLearningApp.Domain.Entities;

/// <summary>One submitted attempt. Append-only: it is the score history.</summary>
public class TestAttempt
{
    private TestAttempt() { }

    internal TestAttempt(
        QuestionSet set, string userId, int score, int totalQuestions, IReadOnlyDictionary<Guid, int> answers)
    {
        DomainException.Require(totalQuestions > 0, "An attempt must cover at least one question.");
        DomainException.Require(score >= 0 && score <= totalQuestions, "Score must fall within the question count.");

        QuestionSetId = set.Id;
        QuestionSet = set;
        UserId = DomainException.RequireText(userId, "User", 450);
        Score = score;
        TotalQuestions = totalQuestions;
        Answers = new Dictionary<Guid, int>(answers);
        SubmittedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid QuestionSetId { get; private set; }
    public QuestionSet? QuestionSet { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    public int Score { get; private set; }
    public int TotalQuestions { get; private set; }
    public IReadOnlyDictionary<Guid, int> Answers { get; private set; } = new Dictionary<Guid, int>();
    public DateTimeOffset SubmittedAt { get; private set; }

    public double Percentage => TotalQuestions == 0 ? 0 : (double)Score / TotalQuestions * 100d;
}
