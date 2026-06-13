using Galvao.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Galvao.Infrastructure.Persistence.Configurations;

public class ConsentLogConfiguration : IEntityTypeConfiguration<ConsentLog>
{
    public void Configure(EntityTypeBuilder<ConsentLog> builder)
    {
        builder.ToTable("ConsentLogs");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.MemberId)
            .IsRequired();

        builder.Property(c => c.Action)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(c => c.IpAddress)
            .HasMaxLength(100);

        builder.Property(c => c.Source)
            .HasMaxLength(500);

        builder.Property(c => c.ConsentToken)
            .HasMaxLength(250);
    }
}
