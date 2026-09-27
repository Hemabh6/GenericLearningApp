namespace GenericLearningApp.Domain.Entities;

/// <summary>One bar on the Gantt: a piece of work on a lane, spanning a range of units.</summary>
public class PlanTask
{
    private PlanTask() { }

    internal PlanTask(Plan plan, Phase? phase, string title, string lane, int startUnit, int endUnit)
    {
        plan.RequireWithin(startUnit, "Task start");
        plan.RequireWithin(endUnit, "Task end");
        DomainException.Require(startUnit <= endUnit, "A task cannot end before it starts.");

        PlanId = plan.Id;
        Plan = plan;
        PhaseId = phase?.Id;
        Phase = phase;
        Title = DomainException.RequireText(title, "Task title", 300);
        Lane = DomainException.RequireText(lane, "Lane", 60);
        StartUnit = startUnit;
        EndUnit = endUnit;
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid PlanId { get; private set; }
    public Plan? Plan { get; private set; }
    public Guid? PhaseId { get; private set; }
    public Phase? Phase { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Lane { get; private set; } = string.Empty;
    public int StartUnit { get; private set; }
    public int EndUnit { get; private set; }
    public bool IsDone { get; private set; }

    public void Edit(string title, string lane, int startUnit, int endUnit)
    {
        DomainException.Require(startUnit <= endUnit, "A task cannot end before it starts.");
        Plan?.RequireWithin(startUnit, "Task start");
        Plan?.RequireWithin(endUnit, "Task end");
        Title = DomainException.RequireText(title, "Task title", 300);
        Lane = DomainException.RequireText(lane, "Lane", 60);
        StartUnit = startUnit;
        EndUnit = endUnit;
    }

    public void SetDone(bool isDone) => IsDone = isDone;

    internal void ClampTo(int length)
    {
        StartUnit = Math.Clamp(StartUnit, 1, length);
        EndUnit = Math.Clamp(EndUnit, StartUnit, length);
    }
}
