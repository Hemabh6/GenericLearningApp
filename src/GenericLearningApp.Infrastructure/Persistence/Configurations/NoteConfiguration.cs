using GenericLearningApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GenericLearningApp.Infrastructure.Persistence.Configurations;

public class NoteConfiguration : IEntityTypeConfiguration<Note>
{
    public void Configure(EntityTypeBuilder<Note> builder)
    {
        builder.ToTable("notes");
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Title).HasMaxLength(Note.TitleMaxLength).IsRequired();
        builder.Property(n => n.Body).IsRequired();

        builder.HasOne(n => n.Subject).WithMany()
            .HasForeignKey(n => n.SubjectId).OnDelete(DeleteBehavior.Cascade);

        // The notes list is newest-edited first.
        builder.HasIndex(n => new { n.SubjectId, n.UpdatedAt });
    }
}

public class ReferenceTermConfiguration : IEntityTypeConfiguration<ReferenceTerm>
{
    public void Configure(EntityTypeBuilder<ReferenceTerm> builder)
    {
        builder.ToTable("reference_terms");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Term).HasMaxLength(200).IsRequired();
        builder.Property(r => r.Meaning).HasMaxLength(1000).IsRequired();
        builder.Property(r => r.UseItWhen).HasMaxLength(1000).IsRequired();

        builder.HasOne(r => r.Subject).WithMany()
            .HasForeignKey(r => r.SubjectId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => new { r.SubjectId, r.SortOrder });
    }
}
