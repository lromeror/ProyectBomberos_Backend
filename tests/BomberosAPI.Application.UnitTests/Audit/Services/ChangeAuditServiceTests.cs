using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BomberosAPI.Application.Features.Audit;
using BomberosAPI.Domain.Entities;
using BomberosAPI.Domain.Repositories;
using FluentAssertions;
using Moq;
using Xunit;

namespace BomberosAPI.Application.UnitTests.Audit.Services;

public class ChangeAuditServiceTests
{
    private readonly Mock<IChangeAuditRepository> _mockRepo;
    private readonly ChangeAuditService _sut;

    public ChangeAuditServiceTests()
    {
        _mockRepo = new Mock<IChangeAuditRepository>();
        _sut = new ChangeAuditService(_mockRepo.Object);
    }

    [Fact]
    public async Task LogAsync_SerializesValuesAndPersistsEntry()
    {
        var actorId = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        ChangeAudit? captured = null;
        _mockRepo.Setup(r => r.AddAsync(It.IsAny<ChangeAudit>(), It.IsAny<CancellationToken>()))
            .Callback<ChangeAudit, CancellationToken>((entry, _) => captured = entry)
            .Returns(Task.CompletedTask);

        await _sut.LogAsync(actorId, "User", entityId, "StatusChange",
            new Dictionary<string, object?> { ["status"] = "active" },
            new Dictionary<string, object?> { ["status"] = "inactive" });

        captured.Should().NotBeNull();
        captured!.ActorUserId.Should().Be(actorId);
        captured.Entity.Should().Be("User");
        captured.EntityId.Should().Be(entityId);
        captured.Operation.Should().Be("StatusChange");
        captured.PreviousValuesJson.Should().Contain("active");
        captured.NewValuesJson.Should().Contain("inactive");
    }

    [Fact]
    public async Task LogAsync_NullValues_DoesNotThrow()
    {
        var act = async () => await _sut.LogAsync(Guid.NewGuid(), "MedicalHistory", Guid.NewGuid(), "Create", null, null);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task GetRecentAsync_ReturnsMappedDtos()
    {
        var entries = new List<ChangeAudit>
        {
            new ChangeAudit { ChangeAuditId = Guid.NewGuid(), Entity = "User", Operation = "StatusChange", OccurredAt = DateTime.UtcNow }
        };
        _mockRepo.Setup(r => r.GetRecentAsync(null, It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(entries);

        var result = await _sut.GetRecentAsync(null);

        result.Should().HaveCount(1);
        result[0].Entity.Should().Be("User");
    }
}
