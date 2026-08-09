using FluentValidation;

namespace BomberosAPI.Application.Features.TrainingSessions;

public class UpdateTrainingSessionValidator : AbstractValidator<UpdateTrainingSessionRequest>
{
    public UpdateTrainingSessionValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ScheduledStart).NotEmpty();
        RuleFor(x => x.ScheduledEnd).NotEmpty()
            .GreaterThan(x => x.ScheduledStart).WithMessage("ScheduledEnd must be after ScheduledStart.");
        RuleFor(x => x.PlannedCapacity).GreaterThan(0).When(x => x.PlannedCapacity is not null);
    }
}
