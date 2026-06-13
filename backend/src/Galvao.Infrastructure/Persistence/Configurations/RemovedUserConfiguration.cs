using Galvao.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Galvao.Infrastructure.Persistence.Configurations;

public class RemovedUserConfiguration : IEntityTypeConfiguration<RemovedUser>
{
    public void Configure(EntityTypeBuilder<RemovedUser> builder)
    {
        builder.ToTable("RemovedUsers");

        builder.HasKey(ru => ru.Id);

        builder.Property(ru => ru.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(ru => ru.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(ru => ru.RemovedPersonalInformation)
            .IsRequired();

        builder.Property(ru => ru.RemovedAccountData)
            .IsRequired();

        builder.Property(ru => ru.RemovedMarketData)
            .IsRequired();

        builder.Property(ru => ru.RemovedFromMailProvider)
            .IsRequired();
    }
}
