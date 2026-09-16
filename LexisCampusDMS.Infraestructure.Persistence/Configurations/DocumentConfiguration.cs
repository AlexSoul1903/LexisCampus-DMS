using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LexisCampusDMS.Core.Domain.Entities;

namespace LexisCampusDMS.Infraestructure.Persistence.Configurations;

public class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.ToTable("Documents");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(d => d.StudentRegistration)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(d => d.DocumentType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(d => d.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(d => d.CurrentVersion)
            .IsRequired();

        builder.Property(d => d.CreatedAtUtc)
            .IsRequired();

        builder.Property(d => d.CreatedBy)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(d => d.LastModifiedBy)
            .HasMaxLength(100);

        builder.Property(d => d.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        // Soft-delete global query filter
        builder.HasQueryFilter(d => !d.IsDeleted);

        // Non-clustered high-performance indexes
        builder.HasIndex(d => d.StudentRegistration)
            .HasDatabaseName("IX_Documents_StudentRegistration");

        builder.HasIndex(d => d.DocumentType)
            .HasDatabaseName("IX_Documents_DocumentType");

        builder.HasIndex(d => d.CreatedAtUtc)
            .HasDatabaseName("IX_Documents_CreatedAtUtc");

        builder.HasIndex(d => d.Status)
            .HasDatabaseName("IX_Documents_Status");

        // Relationships
        builder.HasMany(d => d.Versions)
            .WithOne(v => v.Document)
            .HasForeignKey(v => v.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(d => d.AuditLogs)
            .WithOne(a => a.Document)
            .HasForeignKey(a => a.DocumentId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
