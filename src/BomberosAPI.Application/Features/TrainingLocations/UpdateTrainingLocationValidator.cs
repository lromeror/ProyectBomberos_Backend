using FluentValidation;

namespace BomberosAPI.Application.Features.TrainingLocations;

public class UpdateTrainingLocationValidator : AbstractValidator<UpdateTrainingLocationRequest>
{
    public UpdateTrainingLocationValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.LocationType).MaximumLength(50);
        RuleFor(x => x.Address).MaximumLength(300);
        RuleFor(x => x.MaxCapacity).GreaterThan(0).When(x => x.MaxCapacity.HasValue);
    }
}
