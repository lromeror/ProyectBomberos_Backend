namespace BomberosAPI.Domain.Entities;

public class VitalSignsMeasurement
{
    public Guid VitalSignsMeasurementId { get; set; }
    public Guid SessionParticipantId { get; set; }
    public Guid RegisteredByHealthPersonnelId { get; set; }
    public decimal? HeartRate { get; set; }
    public decimal? SystolicPressure { get; set; }
    public decimal? DiastolicPressure { get; set; }
    public decimal? TemperatureC { get; set; }
    public decimal? Spo2 { get; set; }
    public string? PracticeRole { get; set; }
    public bool? IsSmoker { get; set; }
    public bool? ExposedToSmoke48h { get; set; }
    public DateTime TakenAt { get; set; }
}
