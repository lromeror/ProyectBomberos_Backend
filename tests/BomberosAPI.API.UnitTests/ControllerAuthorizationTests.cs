using System.Linq;
using System.Reflection;
using BomberosAPI.API.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BomberosAPI.API.UnitTests;

// Reflection sobre los atributos [Authorize] reales de los controllers compilados —
// sin levantar un host HTTP ni una base de datos. Existe específicamente porque el
// audit encontró que varios GET de datos médicos (MedicalHistory/VitalSigns/
// SymptomReports/Bioimpedance/EnvironmentalData) solo tenían el [Authorize] a nivel de
// clase (cualquier rol autenticado, incluido un aspirante) y ningún test lo detectó —
// ver AUDIT_REPORT.md, "Several medical-data GET endpoints have no role restriction".
// Estas pruebas fijan esa corrección para que una futura reversión accidental falle en
// CI en vez de en producción.
public class ControllerAuthorizationTests
{
    private static AuthorizeAttribute? RoleAuthorize(System.Type controllerType, string actionName)
    {
        var method = controllerType.GetMethod(actionName, BindingFlags.Public | BindingFlags.Instance)!;
        method.Should().NotBeNull($"{controllerType.Name} should declare an action named {actionName}");
        return method.GetCustomAttributes<AuthorizeAttribute>(inherit: false).FirstOrDefault();
    }

    [Theory]
    [InlineData(typeof(MedicalHistoryController), nameof(MedicalHistoryController.GetAll))]
    [InlineData(typeof(MedicalHistoryController), nameof(MedicalHistoryController.GetById))]
    [InlineData(typeof(MedicalHistoryController), nameof(MedicalHistoryController.GetByTrainee))]
    public void MedicalHistoryReads_RequireStaffRole_ExcludingTrainee(System.Type controller, string action)
    {
        var authorize = RoleAuthorize(controller, action);

        authorize.Should().NotBeNull(
            $"{controller.Name}.{action} must not rely only on the class-level [Authorize] — " +
            "that allows any authenticated role, including FIREFIGHTER_TRAINEE, to read every trainee's medical history.");
        authorize!.Roles.Should().NotBeNullOrWhiteSpace();
        authorize.Roles.Should().NotContain("FIREFIGHTER_TRAINEE");
    }

    [Theory]
    [InlineData(typeof(VitalSignsMeasurementsController), nameof(VitalSignsMeasurementsController.GetAll))]
    [InlineData(typeof(VitalSignsMeasurementsController), nameof(VitalSignsMeasurementsController.GetById))]
    [InlineData(typeof(SymptomReportsController), nameof(SymptomReportsController.GetAll))]
    [InlineData(typeof(SymptomReportsController), nameof(SymptomReportsController.GetById))]
    [InlineData(typeof(BioimpedanceMeasurementsController), nameof(BioimpedanceMeasurementsController.GetAll))]
    [InlineData(typeof(BioimpedanceMeasurementsController), nameof(BioimpedanceMeasurementsController.GetById))]
    [InlineData(typeof(EnvironmentalDataController), nameof(EnvironmentalDataController.GetAll))]
    [InlineData(typeof(EnvironmentalDataController), nameof(EnvironmentalDataController.GetById))]
    [InlineData(typeof(EnvironmentalDataController), nameof(EnvironmentalDataController.GetBySession))]
    public void UnfilteredMedicalDataReads_RequireRoleRestriction(System.Type controller, string action)
    {
        // GetAll/GetById devuelven TODOS los registros del sistema sin filtrar por
        // dueño — no deben quedar abiertos a "cualquier autenticado".
        var authorize = RoleAuthorize(controller, action);

        authorize.Should().NotBeNull($"{controller.Name}.{action} returns unfiltered data and must restrict by role.");
        authorize!.Roles.Should().NotBeNullOrWhiteSpace();
    }

    [Theory]
    [InlineData(typeof(VitalSignsMeasurementsController), nameof(VitalSignsMeasurementsController.GetByParticipant))]
    [InlineData(typeof(VitalSignsMeasurementsController), nameof(VitalSignsMeasurementsController.GetByTrainee))]
    [InlineData(typeof(SymptomReportsController), nameof(SymptomReportsController.GetByParticipant))]
    [InlineData(typeof(SymptomReportsController), nameof(SymptomReportsController.GetByTrainee))]
    [InlineData(typeof(BioimpedanceMeasurementsController), nameof(BioimpedanceMeasurementsController.GetByParticipant))]
    [InlineData(typeof(BioimpedanceMeasurementsController), nameof(BioimpedanceMeasurementsController.GetByTrainee))]
    public void OwnDataReads_AllowStaffAndFirefighterTraineeSelfAccess(System.Type controller, string action)
    {
        // A diferencia del grupo anterior, estas rutas SÍ las llama el propio
        // aspirante para ver sus datos (TraineeDashboard/ProgressHistoryScreen) — el
        // rol debe seguir permitido acá, a diferencia de GetAll/GetById.
        var authorize = RoleAuthorize(controller, action);

        authorize.Should().NotBeNull();
        authorize!.Roles.Should().Contain("FIREFIGHTER_TRAINEE");
        authorize.Roles.Should().Contain("MEDICAL");
    }

    [Fact]
    public void EveryControllerRequiresAuthenticationByDefault()
    {
        // AuthController es la única excepción deliberada: mezcla acciones anónimas
        // (login, forgot/reset-password) con acciones que sí exigen [Authorize] propio
        // (me, logout, change-password) en vez de un [Authorize] a nivel de clase.
        var controllerTypes = typeof(AuthController).Assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(ControllerBase).IsAssignableFrom(t))
            .Where(t => t != typeof(AuthController));

        foreach (var type in controllerTypes)
        {
            type.GetCustomAttributes<AuthorizeAttribute>(inherit: false).Any()
                .Should().BeTrue($"{type.Name} should carry a class-level [Authorize] — anonymous actions must opt out explicitly per action.");
        }
    }
}
