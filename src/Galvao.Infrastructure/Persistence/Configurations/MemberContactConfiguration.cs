using Galvao.Domain.Entities;
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

        builder.Property(mc => mc.Unsubscribed)
            .IsRequired();

        builder.HasOne<Member>()
            .WithOne()
            .HasForeignKey<MemberContact>(mc => mc.Id)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
