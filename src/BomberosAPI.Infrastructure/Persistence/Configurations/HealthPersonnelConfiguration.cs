using BomberosAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BomberosAPI.Infrastructure.Persistence.Configurations;

public class HealthPersonnelConfiguration : IEntityTypeConfiguration<HealthPersonnel>
{
    public void Configure(EntityTypeBuilder<HealthPersonnel> builder)
    {
        builder.ToTable("HealthPersonnel");
        builder.HasKey(e => e.HealthPersonnelId);
        builder.Property(e => e.HealthPersonnelId).HasColumnName("health_personnel_id");
        builder.Property(e => e.UserId).HasColumnName("user_id");
        builder.Property(e => e.Profession).HasColumnName("profession").HasMaxLength(100);
        builder.Property(e => e.Specialty).HasColumnName("specialty").HasMaxLength(100);
        builder.Property(e => e.LicenseNumber).HasColumnName("license_number").HasMaxLength(50);
        builder.Property(e => e.CanApproveDischarges).HasColumnName("can_approve_discharges");
        builder.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
    }
}
