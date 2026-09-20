using GenericLearningApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GenericLearningApp.Infrastructure.Persistence.Configurations;

public class SiteSettingsConfiguration : IEntityTypeConfiguration<SiteSettings>
{
    public void Configure(EntityTypeBuilder<SiteSettings> builder)
    {
        builder.ToTable("site_settings");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.Property(s => s.Name).HasMaxLength(SiteSettings.NameMaxLength).IsRequired();
        builder.Property(s => s.Tagline).HasMaxLength(SiteSettings.TaglineMaxLength).IsRequired();
    }
}

public class MenuItemConfiguration : IEntityTypeConfiguration<MenuItem>
{
    public void Configure(EntityTypeBuilder<MenuItem> builder)
    {
        builder.ToTable("menu_items");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();
        builder.Property(m => m.Name).HasMaxLength(MenuItem.NameMaxLength).IsRequired();
        builder.Property(m => m.RolesCsv).HasColumnName("roles").HasMaxLength(1000).IsRequired();
        builder.Property(m => m.Url).HasMaxLength(MenuItem.UrlMaxLength);
        builder.Property(m => m.Page).HasConversion<string>().HasMaxLength(20);
        builder.Property(m => m.Access).HasConversion<string>().HasMaxLength(20);
        builder.Ignore(m => m.Roles);

        builder.HasIndex(m => m.Position);
    }
}

public class MenuOverrideConfiguration : IEntityTypeConfiguration<MenuOverride>
{
    public void Configure(EntityTypeBuilder<MenuOverride> builder)
    {
        builder.ToTable("menu_overrides");
        builder.HasKey(o => new { o.MenuItemId, o.UserId });
        builder.Property(o => o.UserId).HasMaxLength(450).IsRequired();
        builder.Property(o => o.Mode).HasConversion<string>().HasMaxLength(10);

        builder.HasOne<MenuItem>().WithMany()
            .HasForeignKey(o => o.MenuItemId).OnDelete(DeleteBehavior.Cascade);

        // "Which exceptions does this person have" is the only lookup.
        builder.HasIndex(o => o.UserId);
    }
}
