using FluentValidation;

namespace BomberosAPI.Application.Features.MedicalHistory;

public class UpdateMedicalHistoryValidator : AbstractValidator<UpdateMedicalHistoryRequest>
{
    public UpdateMedicalHistoryValidator()
    {
        RuleFor(x => x.Allergies).MaximumLength(1000);
        RuleFor(x => x.PreexistingConditions).MaximumLength(2000);
        RuleFor(x => x.CurrentMedication).MaximumLength(1000);
        RuleFor(x => x.GeneralObservations).MaximumLength(2000);
    }
}
