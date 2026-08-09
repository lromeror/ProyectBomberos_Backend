using FluentValidation;

namespace BomberosAPI.Application.Features.Institutions;

public class UpdateInstitutionValidator : AbstractValidator<UpdateInstitutionRequest>
{
    public UpdateInstitutionValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Acronym).MaximumLength(20);
        RuleFor(x => x.Country).MaximumLength(100);
        RuleFor(x => x.City).MaximumLength(100);
    }
}
