using BomberosAPI.Domain.Enums;
using FluentValidation;

namespace BomberosAPI.Application.Features.SymptomReports;

public class CreateSymptomReportValidator : AbstractValidator<CreateSymptomReportRequest>
{
    private static readonly string[] ValidSeverities = Enum.GetNames(typeof(SymptomSeverity));

    public CreateSymptomReportValidator()
    {
        RuleFor(x => x.SessionParticipantId).NotEmpty();
        RuleFor(x => x.ReportedByUserId).NotEmpty();

        RuleFor(x => x.Severity)
            .Must(s => ValidSeverities.Contains(s))
            .When(x => x.Severity is not null)
            .WithMessage($"Severity must be one of: {string.Join(", ", ValidSeverities)}.");
    }
}
