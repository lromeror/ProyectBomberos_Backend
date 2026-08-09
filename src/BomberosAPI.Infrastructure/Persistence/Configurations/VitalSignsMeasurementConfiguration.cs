using BomberosAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BomberosAPI.Infrastructure.Persistence.Configurations;

public class VitalSignsMeasurementConfiguration : IEntityTypeConfiguration<VitalSignsMeasurement>
{
    public void Configure(EntityTypeBuilder<VitalSignsMeasurement> builder)
    {
        builder.ToTable("VitalSignsMeasurement");
        builder.HasKey(e => e.VitalSignsMeasurementId);
        builder.Property(e => e.VitalSignsMeasurementId).HasColumnName("vital_signs_measurement_id");
        builder.Property(e => e.SessionParticipantId).HasColumnName("session_participant_id");
        builder.Property(e => e.RegisteredByHealthPersonnelId).HasColumnName("registered_by_health_personnel_id");
        builder.Property(e => e.HeartRate).HasColumnName("heart_rate").HasPrecision(5, 2);
        builder.Property(e => e.SystolicPressure).HasColumnName("systolic_pressure").HasPrecision(5, 2);
        builder.Property(e => e.DiastolicPressure).HasColumnName("diastolic_pressure").HasPrecision(5, 2);
        builder.Property(e => e.TemperatureC).HasColumnName("temperature_c").HasPrecision(4, 2);
        // decimal(4,2) solo llega a 99.99: un Spo2 = 100 (lectura sana y común) tiraba un
        // overflow de SQL Server al guardar. Se amplía a (5,2) para admitir 100.00.
        builder.Property(e => e.Spo2).HasColumnName("spo2").HasPrecision(5, 2);
        builder.Property(e => e.PracticeRole).HasColumnName("practice_role").HasMaxLength(30);
        builder.Property(e => e.IsSmoker).HasColumnName("is_smoker");
        builder.Property(e => e.ExposedToSmoke48h).HasColumnName("exposed_to_smoke_48h");
        builder.Property(e => e.TakenAt).HasColumnName("taken_at");
    }
}
