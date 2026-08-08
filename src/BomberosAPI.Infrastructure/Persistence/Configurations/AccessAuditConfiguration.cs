using BomberosAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BomberosAPI.Infrastructure.Persistence.Configurations;

public class AccessAuditConfiguration : IEntityTypeConfiguration<AccessAudit>
{
    public void Configure(EntityTypeBuilder<AccessAudit> builder)
    {
        builder.ToTable("AccessAudit");
        builder.HasKey(e => e.AccessAuditId);
        builder.Property(e => e.AccessAuditId).HasColumnName("access_audit_id");
        builder.Property(e => e.UserId).HasColumnName("user_id");
        builder.Property(e => e.AuthSessionId).HasColumnName("auth_session_id");
        builder.Property(e => e.Event).HasColumnName("event").HasMaxLength(100).IsRequired();
        builder.Property(e => e.ResourceType).HasColumnName("resource_type").HasMaxLength(100);
        builder.Property(e => e.ResourceId).HasColumnName("resource_id");
        builder.Property(e => e.Ip).HasColumnName("ip").HasMaxLength(45);
        builder.Property(e => e.UserAgent).HasColumnName("user_agent").HasMaxLength(500);
        builder.Property(e => e.Success).HasColumnName("success");
        builder.Property(e => e.OccurredAt).HasColumnName("occurred_at");
    }
}
