using Galvao.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Galvao.Infrastructure.Persistence.Configurations;

public class ShowroomItemPhotoConfiguration : IEntityTypeConfiguration<ShowroomItemPhoto>
{
    public void Configure(EntityTypeBuilder<ShowroomItemPhoto> builder)
    {
        builder.ToTable("ShowroomItemPhotos");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Url)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(p => p.Caption)
            .HasMaxLength(500);

        builder.Property(p => p.IsPrimary)
            .IsRequired();
    }
}
