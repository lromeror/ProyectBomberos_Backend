using System;
using System.Threading;
using System.Threading.Tasks;
using BomberosAPI.Application.Common.Exceptions;
using BomberosAPI.Application.Features.SessionResults;
using BomberosAPI.Domain.Entities;
using BomberosAPI.Domain.Repositories;
using FluentAssertions;
using Moq;
using Xunit;

namespace BomberosAPI.Application.UnitTests.SessionResults.Services;

public class SessionResultServiceTests
{
    private readonly Mock<ISessionResultRepository> _mockRepo;
    private readonly Mock<ISessionParticipantRepository> _mockParticipantRepo;
    private readonly SessionResultService _sut;

    public SessionResultServiceTests()
    {
        _mockRepo = new Mock<ISessionResultRepository>();
        _mockParticipantRepo = new Mock<ISessionParticipantRepository>();
        _mockParticipantRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SessionParticipant());
        _sut = new SessionResultService(_mockRepo.Object, _mockParticipantRepo.Object);
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_CreatesResultWithValidator()
    {
        var participantId = Guid.NewGuid();
        var validatorId = Guid.NewGuid();
        var request = new CreateSessionResultRequest(participantId, 87.5m, "Low", true, "Todo dentro de rango.");

        var result = await _sut.CreateAsync(request, validatorId);

        result.SessionParticipantId.Should().Be(participantId);
        result.ValidatedByUserId.Should().Be(validatorId);
        result.FitToContinue.Should().BeTrue();
        _mockRepo.Verify(r => r.AddAsync(It.IsAny<SessionResult>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_InvalidRiskClassification_ThrowsBusinessRuleException()
    {
        var request = new CreateSessionResultRequest(Guid.NewGuid(), null, "Catastrophic", true, null);

        var act = async () => await _sut.CreateAsync(request, Guid.NewGuid());

        await act.Should().ThrowAsync<BusinessRuleException>();
    }

    [Fact]
    public async Task CreateAsync_NullRiskClassification_IsAllowed()
    {
        var request = new CreateSessionResultRequest(Guid.NewGuid(), null, null, false, null);

        var result = await _sut.CreateAsync(request, Guid.NewGuid());

        result.RiskClassification.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_ParticipantNotFound_ThrowsNotFoundException()
    {
        var participantId = Guid.NewGuid();
        _mockParticipantRepo.Setup(r => r.GetByIdAsync(participantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SessionParticipant?)null);
        var request = new CreateSessionResultRequest(participantId, null, null, true, null);

        var act = async () => await _sut.CreateAsync(request, Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetByParticipantAsync_ReturnsMappedDtos()
    {
        var participantId = Guid.NewGuid();
        var items = new System.Collections.Generic.List<SessionResult>
        {
            new SessionResult { SessionResultId = Guid.NewGuid(), SessionParticipantId = participantId, FitToContinue = true }
        };
        _mockRepo.Setup(r => r.GetByParticipantAsync(participantId, It.IsAny<CancellationToken>())).ReturnsAsync(items);

        var result = await _sut.GetByParticipantAsync(participantId);

        result.Should().HaveCount(1);
    }
}
