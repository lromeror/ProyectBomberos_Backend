using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BomberosAPI.Application.Features.Auth;
using BomberosAPI.Domain.Entities;
using BomberosAPI.Domain.Repositories;
using FluentAssertions;
using Moq;
using Xunit;

namespace BomberosAPI.Application.UnitTests.Auth.Services;

public class AuthSessionServiceTests
{
    private readonly Mock<IAuthSessionRepository> _mockRepo;
    private readonly AuthSessionService _sut;

    public AuthSessionServiceTests()
    {
        _mockRepo = new Mock<IAuthSessionRepository>();
        _sut = new AuthSessionService(_mockRepo.Object);
    }

    [Fact]
    public async Task RecordLoginAsync_PersistsActiveSessionWithCapturedMetadata()
    {
        var userId = Guid.NewGuid();
        AuthSession? captured = null;
        _mockRepo.Setup(r => r.AddAsync(It.IsAny<AuthSession>(), It.IsAny<CancellationToken>()))
            .Callback<AuthSession, CancellationToken>((s, _) => captured = s)
            .Returns(Task.CompletedTask);

        var expiresAt = DateTime.UtcNow.AddHours(1);
        await _sut.RecordLoginAsync(userId, "Tablet", "10.0.0.5", "Mozilla/5.0", expiresAt);

        captured.Should().NotBeNull();
        captured!.UserId.Should().Be(userId);
        captured.Status.Should().Be("Active");
        captured.Ip.Should().Be("10.0.0.5");
        captured.ExpiresAt.Should().Be(expiresAt);
        captured.RefreshTokenHash.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task CloseAllForUserAsync_ClosesEveryActiveSession()
    {
        var userId = Guid.NewGuid();
        var active = new List<AuthSession>
        {
            new AuthSession { AuthSessionId = Guid.NewGuid(), UserId = userId, Status = "Active" },
            new AuthSession { AuthSessionId = Guid.NewGuid(), UserId = userId, Status = "Active" },
        };
        _mockRepo.Setup(r => r.GetActiveByUserIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(active);

        await _sut.CloseAllForUserAsync(userId);

        active.Should().OnlyContain(s => s.Status == "Closed" && s.ClosedAt != null);
        _mockRepo.Verify(r => r.UpdateAsync(It.IsAny<AuthSession>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
}
