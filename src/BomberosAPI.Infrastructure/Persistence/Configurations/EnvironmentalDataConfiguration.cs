using BomberosAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BomberosAPI.Infrastructure.Persistence.Configurations;

public class EnvironmentalDataConfiguration : IEntityTypeConfiguration<EnvironmentalData>
{
    public void Configure(EntityTypeBuilder<EnvironmentalData> builder)
    {
        builder.ToTable("EnvironmentalData");
        builder.HasKey(e => e.EnvironmentalDataId);
        builder.Property(e => e.EnvironmentalDataId).HasColumnName("environmental_data_id");
        builder.Property(e => e.TrainingSessionId).HasColumnName("training_session_id");
        builder.Property(e => e.RegisteredByUserId).HasColumnName("registered_by_user_id");
        builder.Property(e => e.TemperatureC).HasColumnName("temperature_c").HasPrecision(4, 2);
        // decimal(4,2) solo llega a 99.99: 100% de humedad relativa (lluvia/ambiente
        // saturado, un valor real) tiraba un overflow de SQL Server al guardar.
        builder.Property(e => e.HumidityPct).HasColumnName("humidity_pct").HasPrecision(5, 2);
        builder.Property(e => e.CoPpm).HasColumnName("co_ppm").HasPrecision(7, 2);
        builder.Property(e => e.HeatStressIndex).HasColumnName("heat_stress_index").HasPrecision(5, 2);
        builder.Property(e => e.MeasuredAt).HasColumnName("measured_at");
    }
}
