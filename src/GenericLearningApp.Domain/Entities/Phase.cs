namespace GenericLearningApp.Domain.Entities;

/// <summary>A named stretch of the plan, drawn as one stop on the road.</summary>
public class Phase
{
    private Phase() { }

    internal Phase(Plan plan, string name, int startUnit, int endUnit, int sortOrder)
    {
        plan.RequireWithin(startUnit, "Phase start");
        plan.RequireWithin(endUnit, "Phase end");
        DomainException.Require(startUnit <= endUnit, "A phase cannot end before it starts.");

        PlanId = plan.Id;
        Plan = plan;
        Name = DomainException.RequireText(name, "Phase name", 200);
        StartUnit = startUnit;
        EndUnit = endUnit;
        SortOrder = sortOrder;
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid PlanId { get; private set; }
    public Plan? Plan { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public int StartUnit { get; private set; }
    public int EndUnit { get; private set; }
    public int SortOrder { get; private set; }

    public void Edit(string name, int startUnit, int endUnit)
    {
        DomainException.Require(startUnit <= endUnit, "A phase cannot end before it starts.");
        Plan?.RequireWithin(startUnit, "Phase start");
        Plan?.RequireWithin(endUnit, "Phase end");
        Name = DomainException.RequireText(name, "Phase name", 200);
        StartUnit = startUnit;
        EndUnit = endUnit;
    }

    internal void ClampTo(int length)
    {
        StartUnit = Math.Clamp(StartUnit, 1, length);
        EndUnit = Math.Clamp(EndUnit, StartUnit, length);
    }
}
