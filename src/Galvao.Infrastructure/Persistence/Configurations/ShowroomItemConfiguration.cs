using Galvao.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Galvao.Infrastructure.Persistence.Configurations;

public class ShowroomItemConfiguration : IEntityTypeConfiguration<ShowroomItem>
{
    public void Configure(EntityTypeBuilder<ShowroomItem> builder)
    {
        builder.ToTable("ShowroomItems");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.Description)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(s => s.Price)
            .IsRequired()
            .HasConversion<double>(); // SQLite doesn't natively support decimal with precision well, double or standard decimal is fine. But let's use default decimal mapping or HasColumnType("TEXT") or similar. SQLite default decimal works fine. Let's map it standard.

        builder.Property(s => s.Category)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasMany(s => s.Photos)
            .WithOne()
            .HasForeignKey(p => p.ShowroomItemId)
            .OnDelete(DeleteBehavior.Cascade);

        // Map private field _photos as a backing field
        builder.Navigation(s => s.Photos)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
