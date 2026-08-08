namespace BomberosAPI.Application.Features.BioimpedanceMeasurements;

public record CreateBioimpedanceMeasurementRequest(
    Guid SessionParticipantId,
    Guid RegisteredByHealthPersonnelId,
    decimal? WeightKg,
    decimal? FatPercentage,
    decimal? MuscleMassKg,
    decimal? BodyWaterPct,
    decimal? BasalMetabolicRate,
    decimal? MetabolicAgeYears,
    decimal? LactatePreMmol,
    decimal? LactatePostMmol,
    decimal? StroopTimeSeconds,
    int? StroopErrors
);
