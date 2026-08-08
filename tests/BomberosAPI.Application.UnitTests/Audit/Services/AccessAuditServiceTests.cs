using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BomberosAPI.Application.Features.Audit;
using BomberosAPI.Domain.Repositories;
using FluentAssertions;
using Moq;
using Xunit;
using AccessAuditEntity = BomberosAPI.Domain.Entities.AccessAudit;

namespace BomberosAPI.Application.UnitTests.Audit.Services;

public class AccessAuditServiceTests
{
    private readonly Mock<IAccessAuditRepository> _mockRepo;
    private readonly AccessAuditService _sut;

    public AccessAuditServiceTests()
    {
        _mockRepo = new Mock<IAccessAuditRepository>();
        _sut = new AccessAuditService(_mockRepo.Object);
    }

    [Fact]
    public async Task LogAsync_ValidRequest_AddsEntryWithActingUserAndDefaultEvent()
    {
        var userId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();
        var request = new LogAccessAuditRequest("MEDICAL_HISTORY", resourceId, null);

        await _sut.LogAsync(userId, request);

        _mockRepo.Verify(r => r.AddAsync(
            It.Is<AccessAuditEntity>(e =>
                e.UserId == userId &&
                e.ResourceType == "MEDICAL_HISTORY" &&
                e.ResourceId == resourceId &&
                e.Event == "ACCESS" &&
                e.Success),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LogAsync_ExplicitAction_UsesRequestedEvent()
    {
        var userId = Guid.NewGuid();
        var request = new LogAccessAuditRequest("MEDICAL_HISTORY", Guid.NewGuid(), "WRITE");

        await _sut.LogAsync(userId, request);

        _mockRepo.Verify(r => r.AddAsync(
            It.Is<AccessAuditEntity>(e => e.Event == "WRITE"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetRecentAsync_ReturnsMappedDtos()
    {
        var entries = new List<AccessAuditEntity>
        {
            new AccessAuditEntity
            {
                AccessAuditId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                ResourceType = "MEDICAL_HISTORY",
                Event = "READ",
                Success = true,
                OccurredAt = DateTime.UtcNow,
            },
        };
        _mockRepo.Setup(r => r.GetRecentAsync("MEDICAL_HISTORY", 200, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entries);

        var result = await _sut.GetRecentAsync("MEDICAL_HISTORY");

        result.Should().HaveCount(1);
        result[0].Event.Should().Be("READ");
        result[0].ResourceType.Should().Be("MEDICAL_HISTORY");
    }
}
