using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LexisCampusDMS.Core.Domain.Entities;

namespace LexisCampusDMS.Infraestructure.Persistence.Configurations;

public class DocumentVersionConfiguration : IEntityTypeConfiguration<DocumentVersion>
{
    public void Configure(EntityTypeBuilder<DocumentVersion> builder)
    {
        builder.ToTable("DocumentVersions");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.VersionNumber)
            .IsRequired();

        builder.Property(v => v.StoragePath)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(v => v.FileHashSha256)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(v => v.FileSize)
            .IsRequired();

        builder.Property(v => v.MimeType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(v => v.CreatedAtUtc)
            .IsRequired();

        builder.Property(v => v.CreatedByUserId)
            .HasMaxLength(100)
            .IsRequired();

        // Unique composite index: each document has unique version numbers
        builder.HasIndex(v => new { v.DocumentId, v.VersionNumber })
            .IsUnique()
            .HasDatabaseName("IX_DocumentVersions_DocumentId_VersionNumber");

        // Non-clustered hash lookup index for deduplication and integrity
        builder.HasIndex(v => v.FileHashSha256)
            .HasDatabaseName("IX_DocumentVersions_FileHashSha256");

        builder.HasIndex(v => v.CreatedAtUtc)
            .HasDatabaseName("IX_DocumentVersions_CreatedAtUtc");
    }
}
