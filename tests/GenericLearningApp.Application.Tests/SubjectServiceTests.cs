using GenericLearningApp.Application.Subjects;
using GenericLearningApp.Domain;
using GenericLearningApp.Domain.Entities;
using GenericLearningApp.Domain.Enums;
using GenericLearningApp.Infrastructure.Subjects;

namespace GenericLearningApp.Application.Tests;

public class SubjectServiceTests : IDisposable
{
    private const string Mine = "user-mine";
    private const string Theirs = "user-theirs";

    private readonly SqliteFixture _db = new();
    private readonly ISubjectService _service;

    public SubjectServiceTests() => _service = new SubjectService(_db);

    [Fact]
    public async Task List_returns_only_my_subjects()
    {
        await _service.CreateAsync(Mine, "Linear algebra");
        await _service.CreateAsync(Mine, "Anthropology");
        await _service.CreateAsync(Theirs, "Their subject");

        var mine = await _service.ListAsync(Mine);

        Assert.Equal(2, mine.Count);
        Assert.DoesNotContain(mine, s => s.Name == "Their subject");
    }

    [Fact]
    public async Task List_is_ordered_by_name()
    {
        await _service.CreateAsync(Mine, "Zoology");
        await _service.CreateAsync(Mine, "Anthropology");

        var mine = await _service.ListAsync(Mine);

        Assert.Equal(["Anthropology", "Zoology"], mine.Select(s => s.Name));
    }

    [Fact]
    public async Task A_blank_name_is_refused()
        => await Assert.ThrowsAsync<DomainException>(() => _service.CreateAsync(Mine, "   "));

    [Fact]
    public async Task Another_users_subject_is_invisible()
    {
        var theirs = await _service.CreateAsync(Theirs, "Their subject");

        Assert.False(await _service.OwnsAsync(Mine, theirs));
        Assert.Null(await _service.GetSummaryAsync(Mine, theirs));
        Assert.False(await _service.RenameAsync(Mine, theirs, "Mine now"));
        Assert.False(await _service.DeleteAsync(Mine, theirs));
    }

    [Fact]
    public async Task A_failed_rename_changes_nothing()
    {
        var theirs = await _service.CreateAsync(Theirs, "Their subject");

        await _service.RenameAsync(Mine, theirs, "Mine now");

        var summary = await _service.GetSummaryAsync(Theirs, theirs);
        Assert.Equal("Their subject", summary!.Name);
    }

    [Fact]
    public async Task Summary_counts_progress_across_the_subject()
    {
        var subjectId = await _service.CreateAsync(Mine, "Linear algebra");

        await using (var db = _db.CreateDbContext())
        {
            var node = new ContentNode(subjectId, "Vectors");
            db.ContentNodes.Add(node);

            var watched = new ContentItem(subjectId, "What are vectors?", ContentKind.Video);
            watched.SetDone(true);
            db.ContentItems.AddRange(watched, new ContentItem(subjectId, "Span and basis", ContentKind.Video));

            db.Notes.Add(new Note(subjectId, "Session one", "Vectors are arrows."));

            var plan = new Plan(subjectId, "Six weeks", PlanUnit.Week, 6);
            var done = plan.AddTask("Watch the series", "Study", 1, 2);
            done.SetDone(true);
            plan.AddTask("Practice problems", "Practice", 3, 4);
            db.Plans.Add(plan);

            var set = new QuestionSet(subjectId, "Basics");
            var q1 = set.AddQuestion("A vector is?", ["An arrow", "A number"], 0);
            var q2 = set.AddQuestion("A basis is?", ["A set", "A scalar"], 0);
            db.QuestionSets.Add(set);
            set.Submit(Mine, new Dictionary<Guid, int> { [q1.Id] = 0, [q2.Id] = 1 });
            set.Submit(Mine, new Dictionary<Guid, int> { [q1.Id] = 0, [q2.Id] = 0 });

            await db.SaveChangesAsync();
        }

        var summary = await _service.GetSummaryAsync(Mine, subjectId);

        Assert.NotNull(summary);
        Assert.Equal(1, summary.ItemsDone);
        Assert.Equal(2, summary.ItemsTotal);
        Assert.Equal(50, summary.ItemsPercent);
        Assert.Equal(1, summary.TasksDone);
        Assert.Equal(2, summary.TasksTotal);
        Assert.Equal(1, summary.NoteCount);
        Assert.Equal(2, summary.BestScore);
        Assert.Equal(2, summary.BestOutOf);
    }

    [Fact]
    public async Task An_empty_subject_reports_zeroes_rather_than_nulls()
    {
        var subjectId = await _service.CreateAsync(Mine, "Fresh start");

        var summary = await _service.GetSummaryAsync(Mine, subjectId);

        Assert.NotNull(summary);
        Assert.Equal(0, summary.ItemsTotal);
        Assert.Equal(0, summary.TasksTotal);
        Assert.Equal(0, summary.NoteCount);
        Assert.Null(summary.BestScore);
        Assert.Equal(0, summary.ItemsPercent);
    }

    [Fact]
    public async Task Deleting_a_subject_takes_its_contents_with_it()
    {
        var subjectId = await _service.CreateAsync(Mine, "Linear algebra");

        await using (var db = _db.CreateDbContext())
        {
            db.Notes.Add(new Note(subjectId, "Session one", "Vectors are arrows."));
            await db.SaveChangesAsync();
        }

        Assert.True(await _service.DeleteAsync(Mine, subjectId));

        await using (var db = _db.CreateDbContext())
        {
            Assert.Empty(db.Notes.Where(n => n.SubjectId == subjectId));
            Assert.Empty(db.Subjects.Where(s => s.Id == subjectId));
        }
    }

    public void Dispose() => _db.Dispose();
}
