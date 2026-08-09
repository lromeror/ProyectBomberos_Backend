using BomberosAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BomberosAPI.Infrastructure.Persistence.Configurations;

public class BioimpedanceMeasurementConfiguration : IEntityTypeConfiguration<BioimpedanceMeasurement>
{
    public void Configure(EntityTypeBuilder<BioimpedanceMeasurement> builder)
    {
        builder.ToTable("BioimpedanceMeasurement");
        builder.HasKey(e => e.BioimpedanceMeasurementId);
        builder.Property(e => e.BioimpedanceMeasurementId).HasColumnName("bioimpedance_measurement_id");
        builder.Property(e => e.SessionParticipantId).HasColumnName("session_participant_id");
        builder.Property(e => e.RegisteredByHealthPersonnelId).HasColumnName("registered_by_health_personnel_id");
        builder.Property(e => e.WeightKg).HasColumnName("weight_kg").HasPrecision(5, 2);
        // decimal(4,2) solo llega a 99.99: 100% de grasa/agua corporal (límite válido
        // según el validador) tiraba un overflow de SQL Server al guardar.
        builder.Property(e => e.FatPercentage).HasColumnName("fat_percentage").HasPrecision(5, 2);
        builder.Property(e => e.MuscleMassKg).HasColumnName("muscle_mass_kg").HasPrecision(5, 2);
        builder.Property(e => e.BodyWaterPct).HasColumnName("body_water_pct").HasPrecision(5, 2);
        builder.Property(e => e.BasalMetabolicRate).HasColumnName("basal_metabolic_rate").HasPrecision(7, 2);
        builder.Property(e => e.MetabolicAgeYears).HasColumnName("metabolic_age_years").HasPrecision(5, 2);
        builder.Property(e => e.LactatePreMmol).HasColumnName("lactate_pre_mmol").HasPrecision(4, 2);
        builder.Property(e => e.LactatePostMmol).HasColumnName("lactate_post_mmol").HasPrecision(4, 2);
        builder.Property(e => e.StroopTimeSeconds).HasColumnName("stroop_time_seconds").HasPrecision(6, 2);
        builder.Property(e => e.StroopErrors).HasColumnName("stroop_errors");
        builder.Property(e => e.TakenAt).HasColumnName("taken_at");
    }
}
