using System.Text.Json;
using GenericLearningApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace GenericLearningApp.Infrastructure.Persistence.Configurations;

public class QuestionSetConfiguration : IEntityTypeConfiguration<QuestionSet>
{
    public void Configure(EntityTypeBuilder<QuestionSet> builder)
    {
        builder.ToTable("question_sets");
        builder.HasKey(q => q.Id);
        builder.Property(q => q.Name).HasMaxLength(200).IsRequired();

        builder.HasOne(q => q.Subject).WithMany()
            .HasForeignKey(q => q.SubjectId).OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(q => q.Questions).WithOne(x => x.QuestionSet)
            .HasForeignKey(x => x.QuestionSetId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(q => q.Attempts).WithOne(x => x.QuestionSet)
            .HasForeignKey(x => x.QuestionSetId).OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(q => q.Questions).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(q => q.Attempts).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(q => q.SubjectId);
    }
}

public class QuestionConfiguration : IEntityTypeConfiguration<Question>
{
    private static readonly JsonSerializerOptions Json = new();

    public void Configure(EntityTypeBuilder<Question> builder)
    {
        builder.ToTable("questions");
        builder.HasKey(q => q.Id);
        builder.Property(q => q.Text).HasMaxLength(1000).IsRequired();
        builder.Property(q => q.Explanation).IsRequired();

        // Options are always a short list read and written whole: jsonb, not a child table.
        builder.Property(q => q.Options)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, Json),
                v => JsonSerializer.Deserialize<List<string>>(v, Json) ?? new List<string>(),
                new ValueComparer<IReadOnlyList<string>>(
                    (a, b) => a != null && b != null && a.SequenceEqual(b),
                    v => v.Aggregate(0, (acc, s) => HashCode.Combine(acc, s.GetHashCode())),
                    v => v.ToList()));

        builder.HasIndex(q => new { q.QuestionSetId, q.SortOrder });
    }
}

public class TestAttemptConfiguration : IEntityTypeConfiguration<TestAttempt>
{
    private static readonly JsonSerializerOptions Json = new();

    public void Configure(EntityTypeBuilder<TestAttempt> builder)
    {
        builder.ToTable("test_attempts");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.UserId).HasMaxLength(450).IsRequired();
        builder.Ignore(a => a.Percentage);

        builder.Property(a => a.Answers)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, Json),
                v => JsonSerializer.Deserialize<Dictionary<Guid, int>>(v, Json) ?? new Dictionary<Guid, int>(),
                new ValueComparer<IReadOnlyDictionary<Guid, int>>(
                    (a, b) => a != null && b != null && a.Count == b.Count && !a.Except(b).Any(),
                    v => v.Aggregate(0, (acc, kv) => HashCode.Combine(acc, kv.Key, kv.Value)),
                    v => new Dictionary<Guid, int>(v)));

        // Score history is read newest first.
        builder.HasIndex(a => new { a.QuestionSetId, a.SubmittedAt });
    }
}
