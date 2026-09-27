using GenericLearningApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GenericLearningApp.Infrastructure.Persistence;

/// <summary>
/// Every learning aggregate. Identity lives in its own context: the two share a database
/// but nothing else, so neither migration set has to know about the other.
/// </summary>
public class LearningDbContext(DbContextOptions<LearningDbContext> options) : DbContext(options)
{
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<ContentNode> ContentNodes => Set<ContentNode>();
    public DbSet<ContentItem> ContentItems => Set<ContentItem>();
    public DbSet<Note> Notes => Set<Note>();
    public DbSet<ReferenceTerm> ReferenceTerms => Set<ReferenceTerm>();
    public DbSet<Plan> Plans => Set<Plan>();
    public DbSet<Phase> Phases => Set<Phase>();
    public DbSet<PlanTask> PlanTasks => Set<PlanTask>();
    public DbSet<Checkpoint> Checkpoints => Set<Checkpoint>();
    public DbSet<QuestionSet> QuestionSets => Set<QuestionSet>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<TestAttempt> TestAttempts => Set<TestAttempt>();

    // Site-wide configuration, edited from the admin area.
    public DbSet<SiteSettings> SiteSettings => Set<SiteSettings>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<MenuOverride> MenuOverrides => Set<MenuOverride>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(LearningDbContext).Assembly);
    }
}
