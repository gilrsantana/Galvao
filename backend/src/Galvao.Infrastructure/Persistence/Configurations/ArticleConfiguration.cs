using Galvao.Domain.ArticleAggregate.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Galvao.Infrastructure.Persistence.Configurations;

public class ArticleConfiguration : IEntityTypeConfiguration<Article>
{
    public void Configure(EntityTypeBuilder<Article> builder)
    {
        builder.ToTable("Articles");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Title)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(a => a.Content)
            .IsRequired();

        builder.Property(a => a.Author)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.IsPublished)
            .IsRequired();
    }
}
