namespace BomberosAPI.Domain.Entities;

public class BioimpedanceMeasurement
{
    public Guid BioimpedanceMeasurementId { get; set; }
    public Guid SessionParticipantId { get; set; }
    public Guid RegisteredByHealthPersonnelId { get; set; }
    public decimal? WeightKg { get; set; }
    public decimal? FatPercentage { get; set; }
    public decimal? MuscleMassKg { get; set; }
    public decimal? BodyWaterPct { get; set; }
    public decimal? BasalMetabolicRate { get; set; }

    // Marcadores de investigación (Sprint de investigación): opcionales, capturados
    // junto con la bioimpedancia en la misma sesión de medición.
    public decimal? MetabolicAgeYears { get; set; }
    public decimal? LactatePreMmol { get; set; }
    public decimal? LactatePostMmol { get; set; }
    public decimal? StroopTimeSeconds { get; set; }
    public int? StroopErrors { get; set; }

    public DateTime TakenAt { get; set; }
}
