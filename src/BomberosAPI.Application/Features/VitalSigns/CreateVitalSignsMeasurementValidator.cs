using FluentValidation;

namespace BomberosAPI.Application.Features.VitalSigns;

public class CreateVitalSignsMeasurementValidator : AbstractValidator<CreateVitalSignsMeasurementRequest>
{
    public CreateVitalSignsMeasurementValidator()
    {
        RuleFor(x => x.SessionParticipantId).NotEmpty();
        RuleFor(x => x.RegisteredByHealthPersonnelId).NotEmpty();

        // Rangos fisiológicos razonables (validación sanitaria)
        RuleFor(x => x.HeartRate)
            .InclusiveBetween(20, 250).When(x => x.HeartRate.HasValue)
            .WithMessage("HeartRate must be between 20 and 250 bpm.");

        RuleFor(x => x.SystolicPressure)
            .InclusiveBetween(50, 250).When(x => x.SystolicPressure.HasValue)
            .WithMessage("SystolicPressure must be between 50 and 250 mmHg.");

        RuleFor(x => x.DiastolicPressure)
            .InclusiveBetween(30, 150).When(x => x.DiastolicPressure.HasValue)
            .WithMessage("DiastolicPressure must be between 30 and 150 mmHg.");

        RuleFor(x => x.TemperatureC)
            .InclusiveBetween(30, 45).When(x => x.TemperatureC.HasValue)
            .WithMessage("TemperatureC must be between 30 and 45 °C.");

        RuleFor(x => x.Spo2)
            .InclusiveBetween(50, 100).When(x => x.Spo2.HasValue)
            .WithMessage("Spo2 must be between 50 and 100 %.");

        RuleFor(x => x.PracticeRole).MaximumLength(30);
    }
}