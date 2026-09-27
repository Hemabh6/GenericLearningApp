using GenericLearningApp.Domain;
using GenericLearningApp.Domain.Entities;
using GenericLearningApp.Domain.Enums;

namespace GenericLearningApp.Domain.Tests;

public class PlanTests
{
    private static Plan NewPlan(int length = 12)
        => new(Guid.NewGuid(), "Twelve weeks", PlanUnit.Week, length);

    [Theory]
    [InlineData(0)]
    [InlineData(61)]
    [InlineData(-1)]
    public void Length_outside_one_to_sixty_is_rejected(int length)
        => Assert.Throws<DomainException>(() => new Plan(Guid.NewGuid(), "Bad", PlanUnit.Week, length));

    [Fact]
    public void Phase_cannot_end_before_it_starts()
        => Assert.Throws<DomainException>(() => NewPlan().AddPhase("Foundations", 5, 3));

    [Fact]
    public void Phase_cannot_run_past_the_plan()
        => Assert.Throws<DomainException>(() => NewPlan(12).AddPhase("Revision", 11, 13));

    [Fact]
    public void Task_cannot_run_past_the_plan()
        => Assert.Throws<DomainException>(() => NewPlan(12).AddTask("Past questions", "Practice", 12, 20));

    [Fact]
    public void Checkpoint_must_sit_inside_the_plan()
        => Assert.Throws<DomainException>(() => NewPlan(12).AddCheckpoint("Exam ready", 13, CheckpointShape.Star));

    [Fact]
    public void Task_cannot_borrow_a_phase_from_another_plan()
    {
        var other = NewPlan();
        var foreignPhase = other.AddPhase("Core topics", 4, 7);

        Assert.Throws<DomainException>(() => NewPlan().AddTask("Notes", "Study", 1, 2, foreignPhase));
    }

    [Fact]
    public void Shrinking_the_plan_clamps_children_instead_of_failing()
    {
        var plan = NewPlan(12);
        var phase = plan.AddPhase("Revision", 11, 12);
        var task = plan.AddTask("Full mock test", "Review", 10, 12);
        var checkpoint = plan.AddCheckpoint("Exam ready", 12, CheckpointShape.Star);

        plan.Resize(8);

        Assert.Equal(8, plan.Length);
        Assert.Equal(8, phase.StartUnit);
        Assert.Equal(8, phase.EndUnit);
        Assert.Equal(8, task.EndUnit);
        Assert.Equal(8, checkpoint.AtUnit);
    }

    [Fact]
    public void Changing_the_unit_leaves_every_range_where_it_was()
    {
        var plan = NewPlan(12);
        var task = plan.AddTask("Sources and schedule", "Study", 1, 3);

        plan.SetUnit(PlanUnit.Month);

        Assert.Equal(PlanUnit.Month, plan.Unit);
        Assert.Equal(1, task.StartUnit);
        Assert.Equal(3, task.EndUnit);
    }
}
