using GenericLearningApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GenericLearningApp.Infrastructure.Persistence.Configurations;

public class PlanConfiguration : IEntityTypeConfiguration<Plan>
{
    public void Configure(EntityTypeBuilder<Plan> builder)
    {
        builder.ToTable("plans");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Unit).HasConversion<int>();

        builder.HasOne(p => p.Subject).WithMany()
            .HasForeignKey(p => p.SubjectId).OnDelete(DeleteBehavior.Cascade);

        // The plan is always loaded whole, so its children are mapped to backing fields.
        builder.HasMany(p => p.Phases).WithOne(x => x.Plan)
            .HasForeignKey(x => x.PlanId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(p => p.Tasks).WithOne(x => x.Plan)
            .HasForeignKey(x => x.PlanId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(p => p.Checkpoints).WithOne(x => x.Plan)
            .HasForeignKey(x => x.PlanId).OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(p => p.Phases).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(p => p.Tasks).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(p => p.Checkpoints).UsePropertyAccessMode(PropertyAccessMode.Field);

        // One plan per subject in v1; lift this when multiple roadmaps are wanted.
        builder.HasIndex(p => p.SubjectId).IsUnique();
    }
}

public class PhaseConfiguration : IEntityTypeConfiguration<Phase>
{
    public void Configure(EntityTypeBuilder<Phase> builder)
    {
        builder.ToTable("phases");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.HasIndex(p => new { p.PlanId, p.SortOrder });
    }
}

public class PlanTaskConfiguration : IEntityTypeConfiguration<PlanTask>
{
    public void Configure(EntityTypeBuilder<PlanTask> builder)
    {
        builder.ToTable("plan_tasks");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Title).HasMaxLength(300).IsRequired();
        builder.Property(t => t.Lane).HasMaxLength(60).IsRequired();

        builder.HasOne(t => t.Phase).WithMany()
            .HasForeignKey(t => t.PhaseId).OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(t => new { t.PlanId, t.Lane });
    }
}

public class CheckpointConfiguration : IEntityTypeConfiguration<Checkpoint>
{
    public void Configure(EntityTypeBuilder<Checkpoint> builder)
    {
        builder.ToTable("checkpoints");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Title).HasMaxLength(200).IsRequired();
        builder.Property(c => c.Shape).HasConversion<int>();
        builder.HasIndex(c => new { c.PlanId, c.AtUnit });
    }
}
