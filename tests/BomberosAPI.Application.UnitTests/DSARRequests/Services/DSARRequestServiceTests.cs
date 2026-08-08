using System;
using System.Threading;
using System.Threading.Tasks;
using BomberosAPI.Application.Common.Exceptions;
using BomberosAPI.Application.Features.DSARRequests;
using BomberosAPI.Domain.Entities;
using BomberosAPI.Domain.Repositories;
using FluentAssertions;
using Moq;
using Xunit;

namespace BomberosAPI.Application.UnitTests.DSARRequests.Services;

public class DSARRequestServiceTests
{
    private readonly Mock<IDSARRequestRepository> _mockRepo;
    private readonly DSARRequestService _sut;

    public DSARRequestServiceTests()
    {
        _mockRepo = new Mock<IDSARRequestRepository>();
        _sut = new DSARRequestService(_mockRepo.Object);
    }

    [Fact]
    public async Task CreateAsync_ValidRightType_CreatesPendingRequestWithDeadline()
    {
        var userId = Guid.NewGuid();
        var request = new CreateDSARRequestRequest("Access", "Quiero ver mis datos");

        var result = await _sut.CreateAsync(userId, request);

        result.Status.Should().Be("Pending");
        result.UserId.Should().Be(userId);
        result.LegalDeadlineAt.Should().NotBeNull();
        result.LegalDeadlineAt!.Value.Should().BeAfter(DateTime.UtcNow);
        _mockRepo.Verify(r => r.AddAsync(It.IsAny<DSARRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_InvalidRightType_ThrowsBusinessRuleException()
    {
        var act = async () => await _sut.CreateAsync(Guid.NewGuid(), new CreateDSARRequestRequest("Whatever", null));

        await act.Should().ThrowAsync<BusinessRuleException>();
    }

    [Fact]
    public async Task RespondAsync_ValidStatus_UpdatesRequestAndSetsResponder()
    {
        var id = Guid.NewGuid();
        var managerId = Guid.NewGuid();
        var entity = new DSARRequest { DSARRequestId = id, RightType = "Access", Status = "Pending", RequestedAt = DateTime.UtcNow };
        _mockRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);

        var result = await _sut.RespondAsync(id, managerId, new RespondDSARRequestRequest("Completed", "Datos enviados por correo."));

        result.Status.Should().Be("Completed");
        result.ManagedByUserId.Should().Be(managerId);
        result.RespondedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task RespondAsync_InvalidStatus_ThrowsBusinessRuleException()
    {
        var act = async () => await _sut.RespondAsync(Guid.NewGuid(), Guid.NewGuid(), new RespondDSARRequestRequest("Whatever", null));

        await act.Should().ThrowAsync<BusinessRuleException>();
    }

    [Fact]
    public async Task RespondAsync_NotFound_ThrowsNotFoundException()
    {
        var act = async () => await _sut.RespondAsync(Guid.NewGuid(), Guid.NewGuid(), new RespondDSARRequestRequest("Completed", null));

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
