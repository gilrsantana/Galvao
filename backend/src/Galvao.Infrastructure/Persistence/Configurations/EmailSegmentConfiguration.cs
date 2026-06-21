using Galvao.Domain.MemberContactAggregate.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Galvao.Infrastructure.Persistence.Configurations;

public class EmailSegmentConfiguration : IEntityTypeConfiguration<EmailSegment>
{
    public void Configure(EntityTypeBuilder<EmailSegment> builder)
    {
        builder.ToTable("EmailSegments");

        builder.HasKey(es => es.Id);

        builder.Property(es => es.ESegmentType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(es => es.SubscriptionDate)
            .IsRequired();

        builder.Property(es => es.UnSubscriptionDate);
    }
}
