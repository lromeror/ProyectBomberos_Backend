using BomberosAPI.Application.Features.HealthPersonnel;
using BomberosAPI.Application.Features.Institutions;
using BomberosAPI.Application.Features.MedicalHistory;
using BomberosAPI.Application.Features.TraineeFirefighters;
using BomberosAPI.Application.Features.TrainingLocations;
using BomberosAPI.Application.Features.TrainingSessions;
using BomberosAPI.Application.Features.Users;
using FluentAssertions;
using Xunit;

namespace BomberosAPI.Application.UnitTests;

// Antes de estos validadores, ningún Update*Request tenía validación alguna (ver
// AUDIT_REPORT.md, "Every Update*Request DTO in the backend has zero validation") — el
// auto-validation de FluentValidation.AspNetCore solo actúa si existe un
// AbstractValidator<T> registrado para el tipo; sin uno, el request body pasaba
// directo al servicio sin chequeo. Estas pruebas confirman que cada validador
// efectivamente rechaza los mismos casos inválidos que su Create*Validator hermano ya
// rechazaba, y acepta los válidos.
public class UpdateRequestValidatorsTests
{
    [Fact]
    public void UpdateUserValidator_InvalidEmail_Fails()
    {
        var result = new UpdateUserValidator().Validate(new UpdateUserRequest("Ana", "Ruiz", null, "not-an-email"));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void UpdateUserValidator_ValidRequest_Passes()
    {
        var result = new UpdateUserValidator().Validate(new UpdateUserRequest("Ana", "Ruiz", "0999999999", "ana@example.com"));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void UpdateInstitutionValidator_EmptyName_Fails()
    {
        var result = new UpdateInstitutionValidator().Validate(new UpdateInstitutionRequest("", null, null, null));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void UpdateHealthPersonnelValidator_ProfessionTooLong_Fails()
    {
        var result = new UpdateHealthPersonnelValidator().Validate(
            new UpdateHealthPersonnelRequest(new string('x', 101), null, null, true));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void UpdateMedicalHistoryValidator_AllergiesTooLong_Fails()
    {
        var result = new UpdateMedicalHistoryValidator().Validate(
            new UpdateMedicalHistoryRequest(new string('x', 1001), null, null, null));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void UpdateTraineeFirefighterValidator_BloodTypeTooLong_Fails()
    {
        var result = new UpdateTraineeFirefighterValidator().Validate(
            new UpdateTraineeFirefighterRequest("TOO-LONG-BLOOD-TYPE", null, null));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void UpdateTrainingLocationValidator_ZeroCapacity_Fails()
    {
        var result = new UpdateTrainingLocationValidator().Validate(
            new UpdateTrainingLocationRequest("Sede Norte", null, null, 0));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void UpdateTrainingSessionValidator_ScheduledEndBeforeStart_Fails()
    {
        var start = DateTime.UtcNow;
        var result = new UpdateTrainingSessionValidator().Validate(
            new UpdateTrainingSessionRequest("Capacitación", null, start, start.AddHours(-1), null));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void UpdateTrainingSessionValidator_ValidRequest_Passes()
    {
        var start = DateTime.UtcNow;
        var result = new UpdateTrainingSessionValidator().Validate(
            new UpdateTrainingSessionRequest("Capacitación", null, start, start.AddHours(2), 20));
        result.IsValid.Should().BeTrue();
    }
}
