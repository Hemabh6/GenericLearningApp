namespace GenericLearningApp.Application.Subjects;

/// <summary>A subject as the picker lists it.</summary>
public record SubjectListItem(Guid Id, string Name, DateTimeOffset CreatedAt);

/// <summary>
/// The Home page's numbers, in one round trip. Counts are computed on read: nothing
/// about progress is stored, so nothing can drift.
/// </summary>
public record SubjectSummary(
    Guid Id,
    string Name,
    int ItemsDone,
    int ItemsTotal,
    int TasksDone,
    int TasksTotal,
    int NoteCount,
    int? BestScore,
    int? BestOutOf)
{
    public int ItemsPercent => Percent(ItemsDone, ItemsTotal);
    public int TasksPercent => Percent(TasksDone, TasksTotal);

    private static int Percent(int done, int total)
        => total == 0 ? 0 : (int)Math.Round((double)done / total * 100d);
}
