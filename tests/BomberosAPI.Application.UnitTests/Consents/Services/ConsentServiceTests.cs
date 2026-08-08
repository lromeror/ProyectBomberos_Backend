using System;
using System.Threading;
using System.Threading.Tasks;
using BomberosAPI.Application.Common.Exceptions;
using BomberosAPI.Application.Features.Consents;
using BomberosAPI.Domain.Entities;
using BomberosAPI.Domain.Repositories;
using FluentAssertions;
using Moq;
using Xunit;

namespace BomberosAPI.Application.UnitTests.Consents.Services;

public class ConsentServiceTests
{
    private readonly Mock<IConsentDocumentRepository> _mockDocRepo;
    private readonly Mock<IUserConsentRepository> _mockConsentRepo;
    private readonly Mock<IUserRepository> _mockUserRepo;
    private readonly ConsentService _sut;

    public ConsentServiceTests()
    {
        _mockDocRepo = new Mock<IConsentDocumentRepository>();
        _mockConsentRepo = new Mock<IUserConsentRepository>();
        _mockUserRepo = new Mock<IUserRepository>();
        _sut = new ConsentService(_mockDocRepo.Object, _mockConsentRepo.Object, _mockUserRepo.Object);
    }

    [Fact]
    public async Task GrantAsync_NoExistingConsent_CreatesActiveConsentForUsersInstitution()
    {
        var userId = Guid.NewGuid();
        var docId = Guid.NewGuid();
        var institutionId = Guid.NewGuid();
        var document = new ConsentDocument { ConsentDocumentId = docId, ConsentType = "InformedConsent", Version = "1" };
        var user = new User { UserId = userId, InstitutionId = institutionId };

        _mockDocRepo.Setup(r => r.GetByIdAsync(docId, It.IsAny<CancellationToken>())).ReturnsAsync(document);
        _mockUserRepo.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _mockConsentRepo.Setup(r => r.GetActiveByUserAndDocumentAsync(userId, docId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserConsent?)null);

        var result = await _sut.GrantAsync(userId, docId);

        result.Status.Should().Be("active");
        result.InstitutionId.Should().Be(institutionId);
        _mockConsentRepo.Verify(r => r.AddAsync(It.IsAny<UserConsent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GrantAsync_AlreadyActive_ReturnsExistingWithoutCreatingDuplicate()
    {
        var userId = Guid.NewGuid();
        var docId = Guid.NewGuid();
        var existing = new UserConsent { UserConsentId = Guid.NewGuid(), UserId = userId, ConsentDocumentId = docId, Status = "active" };

        _mockDocRepo.Setup(r => r.GetByIdAsync(docId, It.IsAny<CancellationToken>())).ReturnsAsync(new ConsentDocument { ConsentDocumentId = docId });
        _mockUserRepo.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(new User { UserId = userId });
        _mockConsentRepo.Setup(r => r.GetActiveByUserAndDocumentAsync(userId, docId, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var result = await _sut.GrantAsync(userId, docId);

        result.UserConsentId.Should().Be(existing.UserConsentId);
        _mockConsentRepo.Verify(r => r.AddAsync(It.IsAny<UserConsent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GrantAsync_DocumentNotFound_ThrowsNotFoundException()
    {
        var act = async () => await _sut.GrantAsync(Guid.NewGuid(), Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task RevokeAsync_ActiveConsent_MarksRevoked()
    {
        var userId = Guid.NewGuid();
        var docId = Guid.NewGuid();
        var existing = new UserConsent { UserConsentId = Guid.NewGuid(), UserId = userId, ConsentDocumentId = docId, Status = "active" };
        _mockConsentRepo.Setup(r => r.GetActiveByUserAndDocumentAsync(userId, docId, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        await _sut.RevokeAsync(userId, docId);

        existing.Status.Should().Be("revoked");
        existing.RevokedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task RevokeAsync_NoActiveConsent_ThrowsNotFoundException()
    {
        var act = async () => await _sut.RevokeAsync(Guid.NewGuid(), Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
