using GenericLearningApp.Domain.Enums;

namespace GenericLearningApp.Domain.Entities;

/// <summary>A marker on the timeline: the star, square or diamond above the Gantt.</summary>
public class Checkpoint
{
    private Checkpoint() { }

    internal Checkpoint(Plan plan, string title, int atUnit, CheckpointShape shape)
    {
        plan.RequireWithin(atUnit, "Checkpoint");
        PlanId = plan.Id;
        Plan = plan;
        Title = DomainException.RequireText(title, "Checkpoint title", 200);
        AtUnit = atUnit;
        Shape = shape;
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid PlanId { get; private set; }
    public Plan? Plan { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public int AtUnit { get; private set; }
    public CheckpointShape Shape { get; private set; }
    public bool IsDone { get; private set; }

    public void Edit(string title, int atUnit, CheckpointShape shape)
    {
        Plan?.RequireWithin(atUnit, "Checkpoint");
        Title = DomainException.RequireText(title, "Checkpoint title", 200);
        AtUnit = atUnit;
        Shape = shape;
    }

    public void SetDone(bool isDone) => IsDone = isDone;

    internal void ClampTo(int length) => AtUnit = Math.Clamp(AtUnit, 1, length);
}
