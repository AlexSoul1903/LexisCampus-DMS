using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LexisCampusDMS.Core.Domain.Entities;

namespace LexisCampusDMS.Infraestructure.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.UserId)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(a => a.Action)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(a => a.TimestampUtc)
            .IsRequired();

        builder.Property(a => a.IpAddress)
            .HasMaxLength(50);

        builder.Property(a => a.Details)
            .HasMaxLength(2000);

        // Non-clustered indexes for audit traceability and compliance reports
        builder.HasIndex(a => a.UserId)
            .HasDatabaseName("IX_AuditLogs_UserId");

        builder.HasIndex(a => a.DocumentId)
            .HasDatabaseName("IX_AuditLogs_DocumentId");

        builder.HasIndex(a => a.TimestampUtc)
            .HasDatabaseName("IX_AuditLogs_TimestampUtc");

        builder.HasIndex(a => a.Action)
            .HasDatabaseName("IX_AuditLogs_Action");
    }
}
