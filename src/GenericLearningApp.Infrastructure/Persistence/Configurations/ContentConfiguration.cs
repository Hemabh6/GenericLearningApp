using GenericLearningApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GenericLearningApp.Infrastructure.Persistence.Configurations;

public class ContentNodeConfiguration : IEntityTypeConfiguration<ContentNode>
{
    public void Configure(EntityTypeBuilder<ContentNode> builder)
    {
        builder.ToTable("content_nodes");
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Name).HasMaxLength(ContentNode.NameMaxLength).IsRequired();
        builder.Property(n => n.MaterializedPath).HasMaxLength(2000).IsRequired();
        builder.Ignore(n => n.Depth);

        builder.HasOne(n => n.Subject).WithMany()
            .HasForeignKey(n => n.SubjectId).OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(n => n.Parent).WithMany()
            .HasForeignKey(n => n.ParentId).OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(n => n.Items).WithOne(i => i.Node)
            .HasForeignKey(i => i.NodeId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(n => n.Items).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(n => new { n.SubjectId, n.SortOrder });

        // Reading a subtree is a prefix match on the path, so it needs its own index.
        builder.HasIndex(n => new { n.SubjectId, n.MaterializedPath });
    }
}

public class ContentItemConfiguration : IEntityTypeConfiguration<ContentItem>
{
    public void Configure(EntityTypeBuilder<ContentItem> builder)
    {
        builder.ToTable("content_items");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Title).HasMaxLength(ContentItem.TitleMaxLength).IsRequired();
        builder.Property(i => i.Url).HasMaxLength(2000);
        builder.Property(i => i.Notes).IsRequired();
        builder.Property(i => i.Kind).HasConversion<int>();

        builder.HasOne<Subject>().WithMany()
            .HasForeignKey(i => i.SubjectId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(i => new { i.SubjectId, i.IsDone });
        builder.HasIndex(i => new { i.NodeId, i.SortOrder });
    }
}
