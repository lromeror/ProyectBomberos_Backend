using FluentValidation;

namespace BomberosAPI.Application.Features.TraineeFirefighters;

public class UpdateTraineeFirefighterValidator : AbstractValidator<UpdateTraineeFirefighterRequest>
{
    public UpdateTraineeFirefighterValidator()
    {
        RuleFor(x => x.BloodType).MaximumLength(5);
        RuleFor(x => x.EmergencyContactName).MaximumLength(150);
        RuleFor(x => x.EmergencyContactPhone).MaximumLength(30);
    }
}
