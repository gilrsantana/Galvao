using Galvao.Domain.Entities;
using Galvao.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Galvao.Infrastructure.Persistence.Configurations;

public class MemberConfiguration : IEntityTypeConfiguration<Member>
{
    public void Configure(EntityTypeBuilder<Member> builder)
    {
        builder.ToTable("Members");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.DisplayName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(m => m.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(m => m.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(m => m.AcceptNews)
            .IsRequired();

        builder.Property(m => m.AcceptPromo)
            .IsRequired();

        builder.Property(m => m.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.HasIndex(m => m.Email)
            .IsUnique();

        builder.HasOne<Account>()
            .WithOne()
            .HasForeignKey<Member>(m => m.Id)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
