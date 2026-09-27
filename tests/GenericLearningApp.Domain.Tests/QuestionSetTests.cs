using GenericLearningApp.Domain;
using GenericLearningApp.Domain.Entities;

namespace GenericLearningApp.Domain.Tests;

public class QuestionSetTests
{
    private static QuestionSet NewSet()
    {
        var set = new QuestionSet(Guid.NewGuid(), "Study methods");
        set.AddQuestion(
            "Which method means retrieving an answer from memory before checking it?",
            ["Highlighting", "Active recall", "Re-reading", "Copying notes"],
            1,
            "Active recall forces retrieval, which strengthens memory more than passive review.");
        set.AddQuestion(
            "What does spaced repetition change?",
            ["The length of each note", "The order of the syllabus", "The gaps between reviews"],
            2);
        return set;
    }

    [Fact]
    public void Correct_index_must_point_at_a_real_option()
    {
        var set = new QuestionSet(Guid.NewGuid(), "Bad test");
        Assert.Throws<DomainException>(() => set.AddQuestion("Which?", ["A", "B"], 5));
    }

    [Fact]
    public void A_question_needs_at_least_two_options()
    {
        var set = new QuestionSet(Guid.NewGuid(), "Bad test");
        Assert.Throws<DomainException>(() => set.AddQuestion("Which?", ["Only one"], 0));
    }

    [Fact]
    public void Duplicate_options_are_rejected()
    {
        var set = new QuestionSet(Guid.NewGuid(), "Bad test");
        Assert.Throws<DomainException>(() => set.AddQuestion("Which?", ["A", "a"], 0));
    }

    [Fact]
    public void Scoring_counts_only_the_right_answers()
    {
        var set = NewSet();
        var first = set.Questions[0];
        var second = set.Questions[1];

        var attempt = set.Submit("user-1", new Dictionary<Guid, int> { [first.Id] = 1, [second.Id] = 0 });

        Assert.Equal(1, attempt.Score);
        Assert.Equal(2, attempt.TotalQuestions);
        Assert.Equal(50d, attempt.Percentage);
    }

    [Fact]
    public void An_unanswered_question_counts_as_wrong()
    {
        var set = NewSet();
        var attempt = set.Submit("user-1", new Dictionary<Guid, int>());

        Assert.Equal(0, attempt.Score);
        Assert.Equal(2, attempt.TotalQuestions);
    }

    [Fact]
    public void An_empty_test_cannot_be_submitted()
    {
        var set = new QuestionSet(Guid.NewGuid(), "Empty");
        Assert.Throws<DomainException>(() => set.Submit("user-1", new Dictionary<Guid, int>()));
    }
}
