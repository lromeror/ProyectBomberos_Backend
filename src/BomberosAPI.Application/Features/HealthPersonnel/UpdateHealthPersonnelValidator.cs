using FluentValidation;

namespace BomberosAPI.Application.Features.HealthPersonnel;

public class UpdateHealthPersonnelValidator : AbstractValidator<UpdateHealthPersonnelRequest>
{
    public UpdateHealthPersonnelValidator()
    {
        RuleFor(x => x.Profession).MaximumLength(100);
        RuleFor(x => x.Specialty).MaximumLength(100);
        RuleFor(x => x.LicenseNumber).MaximumLength(50);
    }
}
