using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BomberosAPI.Application.Common.Exceptions;
using BomberosAPI.Application.Common.Interfaces;
using BomberosAPI.Application.Features.Invitations;
using BomberosAPI.Domain.Entities;
using BomberosAPI.Domain.Repositories;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Moq;
using Xunit;
using ValidationException = BomberosAPI.Application.Common.Exceptions.ValidationException;

namespace BomberosAPI.Application.UnitTests.Invitations.Services;

public class InvitationServiceTests
{
    private readonly Mock<IInvitationRepository> _mockRepo;
    private readonly Mock<ISessionParticipantRepository> _mockParticipantRepo;
    private readonly Mock<ITraineeFirefighterRepository> _mockTraineeRepo;
    private readonly Mock<IPasswordHasher> _mockHasher;
    private readonly Mock<IValidator<CreateInvitationRequest>> _mockValidator;
    private readonly Mock<ICurrentUserService> _mockCurrentUser;
    private readonly InvitationService _sut;

    public InvitationServiceTests()
    {
        _mockRepo = new Mock<IInvitationRepository>();
        _mockParticipantRepo = new Mock<ISessionParticipantRepository>();
        _mockTraineeRepo = new Mock<ITraineeFirefighterRepository>();
        _mockHasher = new Mock<IPasswordHasher>();
        _mockValidator = new Mock<IValidator<CreateInvitationRequest>>();
        _mockCurrentUser = new Mock<ICurrentUserService>();

        _mockValidator
            .Setup(v => v.ValidateAsync(It.IsAny<CreateInvitationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _mockCurrentUser.SetupGet(c => c.IsAuthenticated).Returns(true);
        _mockCurrentUser.SetupGet(c => c.UserId).Returns(Guid.NewGuid());
        _mockHasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("hashedToken");

        _sut = new InvitationService(
            _mockRepo.Object,
            _mockParticipantRepo.Object,
            _mockTraineeRepo.Object,
            _mockHasher.Object,
            _mockValidator.Object,
            _mockCurrentUser.Object);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsMappedDtos()
    {
        var items = new List<Invitation>
        {
            new Invitation { InvitationId = Guid.NewGuid(), Status = "Pending" }
        };
        _mockRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(items);

        var result = await _sut.GetAllAsync();

        result.Should().NotBeEmpty();
        result[0].Status.Should().Be("Pending");
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        var id = Guid.NewGuid();
        var item = new Invitation { InvitationId = id, Status = "Pending" };
        _mockRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(item);

        var result = await _sut.GetByIdAsync(id);

        result.Should().NotBeNull();
        result.Status.Should().Be("Pending");
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_ReturnsDtoAndToken()
    {
        var request = new CreateInvitationRequest(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "test@test.com", DateTime.UtcNow.AddDays(1));

        var result = await _sut.CreateAsync(request);

        result.Should().NotBeNull();
        result.Invitation.Status.Should().Be("Pending");
        result.PlainToken.Should().NotBeNullOrWhiteSpace();
        _mockRepo.Verify(r => r.AddAsync(It.IsAny<Invitation>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_UnauthenticatedUser_ThrowsUnauthorizedException()
    {
        var request = new CreateInvitationRequest(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "test@test.com", DateTime.UtcNow.AddDays(1));
        _mockCurrentUser.SetupGet(c => c.IsAuthenticated).Returns(false);

        var act = async () => await _sut.CreateAsync(request);

        // Antes lanzaba System.UnauthorizedAccessException, que el middleware global no
        // reconoce como AppException y caía al handler genérico (500 en vez de 401) —
        // ver AUDIT_REPORT.md. Este código es defensivamente inalcanzable en producción
        // (el controller ya exige [Authorize]), pero el tipo debe seguir siendo correcto.
        await act.Should().ThrowAsync<BomberosAPI.Application.Common.Exceptions.UnauthorizedException>();
    }

    [Fact]
    public async Task CreateAsync_InvalidRequest_ThrowsValidationException()
    {
        var request = new CreateInvitationRequest(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "test@test.com", DateTime.UtcNow.AddDays(1));
        var validationResult = new ValidationResult(new[] { new ValidationFailure("TargetEmail", "Invalid") });
        _mockValidator.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(validationResult);

        var act = async () => await _sut.CreateAsync(request);
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task AcceptAsync_ValidPendingInvitation_ChangesStatusToAccepted()
    {
        var id = Guid.NewGuid();
        // El destinatario debe ser quien está autenticado — usa el mismo UserId que
        // configura _mockCurrentUser en el constructor.
        var recipientId = _mockCurrentUser.Object.UserId;
        var inv = new Invitation { InvitationId = id, Status = "Pending", ExpiresAt = DateTime.UtcNow.AddDays(1), TargetUserId = recipientId, TrainingSessionId = Guid.NewGuid() };
        var trainee = new TraineeFirefighter { TraineeFirefighterId = Guid.NewGuid(), UserId = recipientId };
        _mockRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(inv);
        _mockTraineeRepo.Setup(t => t.GetByUserIdAsync(recipientId, It.IsAny<CancellationToken>())).ReturnsAsync(trainee);

        var result = await _sut.AcceptAsync(id);

        inv.Status.Should().Be("Accepted");
        result.Should().NotBeNull();
        _mockParticipantRepo.Verify(p => p.AddAsync(It.IsAny<SessionParticipant>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AcceptAsync_NotTheRecipient_ThrowsForbiddenException()
    {
        var id = Guid.NewGuid();
        var inv = new Invitation { InvitationId = id, Status = "Pending", ExpiresAt = DateTime.UtcNow.AddDays(1), TargetUserId = Guid.NewGuid() };
        _mockRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(inv);

        var act = async () => await _sut.AcceptAsync(id);

        await act.Should().ThrowAsync<ForbiddenException>();
        _mockRepo.Verify(r => r.UpdateAsync(It.IsAny<Invitation>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RejectAsync_NotTheRecipient_ThrowsForbiddenException()
    {
        var id = Guid.NewGuid();
        var inv = new Invitation { InvitationId = id, Status = "Pending", TargetUserId = Guid.NewGuid() };
        _mockRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(inv);

        var act = async () => await _sut.RejectAsync(id);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task AcceptAsync_ExpiredInvitation_ThrowsConflictException()
    {
        var id = Guid.NewGuid();
        var inv = new Invitation { InvitationId = id, Status = "Pending", ExpiresAt = DateTime.UtcNow.AddDays(-1) };
        _mockRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(inv);

        var act = async () => await _sut.AcceptAsync(id);

        await act.Should().ThrowAsync<ConflictException>();
    }
    
    [Fact]
    public async Task AcceptAsync_AlreadyAccepted_ThrowsConflictException()
    {
        var id = Guid.NewGuid();
        var inv = new Invitation { InvitationId = id, Status = "Accepted", ExpiresAt = DateTime.UtcNow.AddDays(1) };
        _mockRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(inv);

        var act = async () => await _sut.AcceptAsync(id);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task RejectAsync_Pending_ChangesStatusToRejected()
    {
        var id = Guid.NewGuid();
        var inv = new Invitation { InvitationId = id, Status = "Pending" };
        _mockRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(inv);

        await _sut.RejectAsync(id);

        inv.Status.Should().Be("Rejected");
    }

    [Fact]
    public async Task RevokeAsync_Pending_ChangesStatusToRevoked()
    {
        var id = Guid.NewGuid();
        var inv = new Invitation { InvitationId = id, Status = "Pending" };
        _mockRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(inv);

        await _sut.RevokeAsync(id);

        inv.Status.Should().Be("Revoked");
    }
}
