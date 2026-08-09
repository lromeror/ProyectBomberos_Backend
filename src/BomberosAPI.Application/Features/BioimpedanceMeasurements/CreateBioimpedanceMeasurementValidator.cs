using FluentValidation;

namespace BomberosAPI.Application.Features.BioimpedanceMeasurements;

public class CreateBioimpedanceMeasurementValidator : AbstractValidator<CreateBioimpedanceMeasurementRequest>
{
    public CreateBioimpedanceMeasurementValidator()
    {
        RuleFor(x => x.SessionParticipantId).NotEmpty();
        RuleFor(x => x.RegisteredByHealthPersonnelId).NotEmpty();

        // Rangos fisiológicos razonables (validación sanitaria), todos opcionales.
        RuleFor(x => x.WeightKg)
            .InclusiveBetween(0, 300).When(x => x.WeightKg.HasValue)
            .WithMessage("WeightKg must be between 0 and 300 kg.");

        RuleFor(x => x.MuscleMassKg)
            .InclusiveBetween(0, 300).When(x => x.MuscleMassKg.HasValue)
            .WithMessage("MuscleMassKg must be between 0 and 300 kg.");

        RuleFor(x => x.BasalMetabolicRate)
            .InclusiveBetween(500, 5000).When(x => x.BasalMetabolicRate.HasValue)
            .WithMessage("BasalMetabolicRate must be between 500 and 5000 kcal/day.");

        RuleFor(x => x.FatPercentage)
            .InclusiveBetween(0, 100).When(x => x.FatPercentage.HasValue)
            .WithMessage("FatPercentage must be between 0 and 100 %.");

        RuleFor(x => x.BodyWaterPct)
            .InclusiveBetween(0, 100).When(x => x.BodyWaterPct.HasValue)
            .WithMessage("BodyWaterPct must be between 0 and 100 %.");

        RuleFor(x => x.MetabolicAgeYears)
            .InclusiveBetween(0, 120).When(x => x.MetabolicAgeYears.HasValue)
            .WithMessage("MetabolicAgeYears must be between 0 and 120.");

        RuleFor(x => x.LactatePreMmol)
            .InclusiveBetween(0, 30).When(x => x.LactatePreMmol.HasValue)
            .WithMessage("LactatePreMmol must be between 0 and 30 mmol/L.");

        RuleFor(x => x.LactatePostMmol)
            .InclusiveBetween(0, 30).When(x => x.LactatePostMmol.HasValue)
            .WithMessage("LactatePostMmol must be between 0 and 30 mmol/L.");

        RuleFor(x => x.StroopTimeSeconds)
            .GreaterThanOrEqualTo(0).When(x => x.StroopTimeSeconds.HasValue)
            .WithMessage("StroopTimeSeconds cannot be negative.");

        RuleFor(x => x.StroopErrors)
            .GreaterThanOrEqualTo(0).When(x => x.StroopErrors.HasValue)
            .WithMessage("StroopErrors cannot be negative.");
    }
}
