using GenericLearningApp.Domain.Enums;

namespace GenericLearningApp.Domain.Entities;

/// <summary>
/// The roadmap: a span of <see cref="Length"/> units holding phases, tasks and checkpoints.
/// It is the aggregate root, because every child's range is only valid against the plan's length.
/// </summary>
public class Plan
{
    public const int MinLength = 1;
    public const int MaxLength = 60;

    private readonly List<Phase> _phases = [];
    private readonly List<PlanTask> _tasks = [];
    private readonly List<Checkpoint> _checkpoints = [];

    private Plan() { }

    public Plan(Guid subjectId, string name, PlanUnit unit, int length)
    {
        DomainException.Require(subjectId != Guid.Empty, "Plan must belong to a subject.");
        SubjectId = subjectId;
        Name = DomainException.RequireText(name, "Plan name", 200);
        Unit = unit;
        Length = RequireLength(length);
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid SubjectId { get; private set; }
    public Subject? Subject { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public PlanUnit Unit { get; private set; }
    public int Length { get; private set; }

    public IReadOnlyCollection<Phase> Phases => _phases;
    public IReadOnlyCollection<PlanTask> Tasks => _tasks;
    public IReadOnlyCollection<Checkpoint> Checkpoints => _checkpoints;

    public void Rename(string name) => Name = DomainException.RequireText(name, "Plan name", 200);

    /// <summary>Changing the unit never moves anything: a 12-week plan becomes a 12-month plan.</summary>
    public void SetUnit(PlanUnit unit) => Unit = unit;

    /// <summary>Shrinking clamps every child into the new span rather than rejecting the change.</summary>
    public void Resize(int length)
    {
        Length = RequireLength(length);
        foreach (var phase in _phases) phase.ClampTo(Length);
        foreach (var task in _tasks) task.ClampTo(Length);
        foreach (var checkpoint in _checkpoints) checkpoint.ClampTo(Length);
    }

    public Phase AddPhase(string name, int startUnit, int endUnit)
    {
        var phase = new Phase(this, name, startUnit, endUnit, _phases.Count);
        _phases.Add(phase);
        return phase;
    }

    public PlanTask AddTask(string title, string lane, int startUnit, int endUnit, Phase? phase = null)
    {
        if (phase is not null)
            DomainException.Require(phase.PlanId == Id, "A task cannot reference another plan's phase.");

        var task = new PlanTask(this, phase, title, lane, startUnit, endUnit);
        _tasks.Add(task);
        return task;
    }

    public Checkpoint AddCheckpoint(string title, int atUnit, CheckpointShape shape)
    {
        var checkpoint = new Checkpoint(this, title, atUnit, shape);
        _checkpoints.Add(checkpoint);
        return checkpoint;
    }

    internal void RequireWithin(int unit, string field)
        => DomainException.Require(unit >= 1 && unit <= Length, $"{field} must fall between 1 and {Length}.");

    private static int RequireLength(int length)
    {
        DomainException.Require(
            length is >= MinLength and <= MaxLength,
            $"Plan length must be between {MinLength} and {MaxLength} units.");
        return length;
    }
}
