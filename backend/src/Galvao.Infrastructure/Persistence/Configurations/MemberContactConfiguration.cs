using Galvao.Domain.MemberContactAggregate.Entities;
using Galvao.Domain.MemberUserAggregate.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Galvao.Infrastructure.Persistence.Configurations;

public class MemberContactConfiguration : IEntityTypeConfiguration<MemberContact>
{
    public void Configure(EntityTypeBuilder<MemberContact> builder)
    {
        builder.ToTable("MemberContacts");

        builder.HasKey(mc => mc.Id);

        builder.Property(mc => mc.ExternalContactId)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(mc => mc.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(mc => mc.EmailProvider)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(mc => mc.PhoneNumber)
            .HasMaxLength(50);

        builder.HasOne<Member>()
            .WithOne()
            .HasForeignKey<MemberContact>(mc => mc.MemberId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(mc => mc.EmailSegments)
            .WithOne(es => es.MemberContact)
            .HasForeignKey(es => es.MemberContactId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(mc => mc.EmailSegments)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
