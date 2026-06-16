using Galvao.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Galvao.Infrastructure.Persistence.Configurations;

public class EmailAuditLogConfiguration : IEntityTypeConfiguration<EmailAuditLog>
{
    public void Configure(EntityTypeBuilder<EmailAuditLog> builder)
    {
        builder.ToTable("EmailAuditLogs");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.RecipientEmail)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(l => l.Subject)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(l => l.EMailProvider)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(l => l.ETypeOfMessage)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(l => l.StatusCode)
            .IsRequired();

        builder.Property(l => l.ExternalMessageId)
            .HasMaxLength(150);

        builder.Property(l => l.ErrorMessage)
            .HasColumnType("text");

        builder.Property(l => l.SentAtUtc)
            .IsRequired();

        builder.HasOne<Member>()
            .WithMany()
            .HasForeignKey(l => l.MemberId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
