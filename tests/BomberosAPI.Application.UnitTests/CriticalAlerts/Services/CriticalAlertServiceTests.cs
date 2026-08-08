using System;
using System.Threading;
using System.Threading.Tasks;
using BomberosAPI.Application.Common.Exceptions;
using BomberosAPI.Application.Features.CriticalAlerts;
using BomberosAPI.Domain.Entities;
using BomberosAPI.Domain.Repositories;
using FluentAssertions;
using Moq;
using Xunit;

namespace BomberosAPI.Application.UnitTests.CriticalAlerts.Services;

public class CriticalAlertServiceTests
{
    private readonly Mock<ICriticalAlertRepository> _mockRepo;
    private readonly Mock<ISessionParticipantRepository> _mockParticipantRepo;
    private readonly CriticalAlertService _sut;

    public CriticalAlertServiceTests()
    {
        _mockRepo = new Mock<ICriticalAlertRepository>();
        _mockParticipantRepo = new Mock<ISessionParticipantRepository>();
        _sut = new CriticalAlertService(_mockRepo.Object, _mockParticipantRepo.Object);
    }

    [Fact]
    public async Task CreateAsync_ValidSeverity_CreatesOpenAlert()
    {
        var participantId = Guid.NewGuid();
        var request = new CreateCriticalAlertRequest(participantId, null, null, null, "AbnormalHeartRate", "High", "FC 190 sostenida");
        _mockParticipantRepo.Setup(r => r.GetByIdAsync(participantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SessionParticipant { SessionParticipantId = participantId });

        var result = await _sut.CreateAsync(request);

        result.Status.Should().Be("Open");
        result.SessionParticipantId.Should().Be(participantId);
        _mockRepo.Verify(r => r.AddAsync(It.IsAny<CriticalAlert>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_InvalidSeverity_ThrowsBusinessRuleException()
    {
        var request = new CreateCriticalAlertRequest(Guid.NewGuid(), null, null, null, "AbnormalHeartRate", "Catastrophic", null);

        var act = async () => await _sut.CreateAsync(request);

        await act.Should().ThrowAsync<BusinessRuleException>();
    }

    [Fact]
    public async Task CreateAsync_ParticipantNotFound_ThrowsNotFoundException()
    {
        var request = new CreateCriticalAlertRequest(Guid.NewGuid(), null, null, null, "AbnormalHeartRate", "High", null);

        var act = async () => await _sut.CreateAsync(request);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task AttendAsync_OpenAlert_MarksAttended()
    {
        var id = Guid.NewGuid();
        var attendedBy = Guid.NewGuid();
        var alert = new CriticalAlert { CriticalAlertId = id, Status = "Open", AlertType = "AbnormalHeartRate", Severity = "High" };
        _mockRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(alert);

        var result = await _sut.AttendAsync(id, attendedBy);

        result.Status.Should().Be("Attended");
        result.AttendedByUserId.Should().Be(attendedBy);
    }

    [Fact]
    public async Task AttendAsync_AlreadyAttended_ThrowsConflictException()
    {
        var id = Guid.NewGuid();
        var alert = new CriticalAlert { CriticalAlertId = id, Status = "Attended", AlertType = "AbnormalHeartRate", Severity = "High" };
        _mockRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(alert);

        var act = async () => await _sut.AttendAsync(id, Guid.NewGuid());

        await act.Should().ThrowAsync<ConflictException>();
    }
}
