using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BomberosAPI.Application.Features.Reports;
using BomberosAPI.Domain.Entities;
using BomberosAPI.Domain.Repositories;
using FluentAssertions;
using Moq;
using Xunit;

namespace BomberosAPI.Application.UnitTests.Reports.Services;

public class ReportServiceTests
{
    private readonly Mock<IVitalSignsMeasurementRepository> _mockVitalsRepo;
    private readonly Mock<ISessionParticipantRepository> _mockParticipantRepo;
    private readonly Mock<ITraineeFirefighterRepository> _mockTraineeRepo;
    private readonly Mock<ITrainingSessionRepository> _mockSessionRepo;
    private readonly ReportService _sut;

    public ReportServiceTests()
    {
        _mockVitalsRepo = new Mock<IVitalSignsMeasurementRepository>();
        _mockParticipantRepo = new Mock<ISessionParticipantRepository>();
        _mockTraineeRepo = new Mock<ITraineeFirefighterRepository>();
        _mockSessionRepo = new Mock<ITrainingSessionRepository>();

        _sut = new ReportService(
            _mockVitalsRepo.Object, _mockParticipantRepo.Object, _mockTraineeRepo.Object, _mockSessionRepo.Object);
    }

    [Fact]
    public async Task GetSummaryAsync_AveragesOnlyPresentValues_AndCountsAllEntities()
    {
        var vitals = new List<VitalSignsMeasurement>
        {
            new VitalSignsMeasurement { VitalSignsMeasurementId = Guid.NewGuid(), HeartRate = 80, TakenAt = DateTime.UtcNow },
            new VitalSignsMeasurement { VitalSignsMeasurementId = Guid.NewGuid(), HeartRate = 100, TakenAt = DateTime.UtcNow },
            new VitalSignsMeasurement { VitalSignsMeasurementId = Guid.NewGuid(), HeartRate = null, TakenAt = DateTime.UtcNow },
        };
        _mockVitalsRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(vitals);
        _mockSessionRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TrainingSession> { new TrainingSession { TrainingSessionId = Guid.NewGuid(), Title = "S1", Status = "Scheduled" } });
        _mockParticipantRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SessionParticipant>());
        _mockTraineeRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TraineeFirefighter>());

        var result = await _sut.GetSummaryAsync(null, null);

        result.TotalSessions.Should().Be(1);
        result.TotalVitalSignsMeasurements.Should().Be(3);
        result.AvgHeartRate.Should().Be(90m);
    }

    [Fact]
    public async Task GetSummaryAsync_FiltersByDateRange()
    {
        var inRange = DateTime.UtcNow;
        var outOfRange = DateTime.UtcNow.AddDays(-10);
        var vitals = new List<VitalSignsMeasurement>
        {
            new VitalSignsMeasurement { VitalSignsMeasurementId = Guid.NewGuid(), HeartRate = 70, TakenAt = inRange },
            new VitalSignsMeasurement { VitalSignsMeasurementId = Guid.NewGuid(), HeartRate = 200, TakenAt = outOfRange },
        };
        _mockVitalsRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(vitals);
        _mockSessionRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<TrainingSession>());
        _mockParticipantRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<SessionParticipant>());
        _mockTraineeRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<TraineeFirefighter>());

        var result = await _sut.GetSummaryAsync(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1));

        result.TotalVitalSignsMeasurements.Should().Be(1);
        result.AvgHeartRate.Should().Be(70m);
    }

    [Fact]
    public async Task GetAnonymizedVitalsExportAsync_NoVitals_ReturnsEmpty()
    {
        _mockVitalsRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<VitalSignsMeasurement>());

        var result = await _sut.GetAnonymizedVitalsExportAsync(null, null);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAnonymizedVitalsExportAsync_UsesApplicantCodeInsteadOfPii()
    {
        var participantId = Guid.NewGuid();
        var traineeId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

        _mockVitalsRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<VitalSignsMeasurement>
        {
            new VitalSignsMeasurement { VitalSignsMeasurementId = Guid.NewGuid(), SessionParticipantId = participantId, HeartRate = 85, TakenAt = DateTime.UtcNow },
        });
        _mockParticipantRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<SessionParticipant>
        {
            new SessionParticipant { SessionParticipantId = participantId, TraineeFirefighterId = traineeId, TrainingSessionId = sessionId },
        });
        _mockTraineeRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<TraineeFirefighter>
        {
            new TraineeFirefighter { TraineeFirefighterId = traineeId, ApplicantCode = "APP-042" },
        });
        _mockSessionRepo.Setup(r => r.GetByIdAsync(sessionId, It.IsAny<CancellationToken>())).ReturnsAsync(
            new TrainingSession { TrainingSessionId = sessionId, Title = "Sesión X", Status = "Scheduled" });

        var result = await _sut.GetAnonymizedVitalsExportAsync(null, null);

        result.Should().HaveCount(1);
        result[0].ApplicantCode.Should().Be("APP-042");
        result[0].SessionTitle.Should().Be("Sesión X");
    }

    [Fact]
    public async Task GetAnonymizedVitalsExportAsync_SkipsRowsWithUnknownParticipantOrTrainee()
    {
        _mockVitalsRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<VitalSignsMeasurement>
        {
            new VitalSignsMeasurement { VitalSignsMeasurementId = Guid.NewGuid(), SessionParticipantId = Guid.NewGuid(), HeartRate = 85, TakenAt = DateTime.UtcNow },
        });
        _mockParticipantRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<SessionParticipant>());
        _mockTraineeRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<TraineeFirefighter>());

        var result = await _sut.GetAnonymizedVitalsExportAsync(null, null);

        result.Should().BeEmpty();
    }
}
