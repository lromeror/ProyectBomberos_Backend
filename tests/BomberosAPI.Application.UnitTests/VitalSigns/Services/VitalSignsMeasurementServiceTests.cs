using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BomberosAPI.Application.Common.Exceptions;
using BomberosAPI.Application.Features.VitalSigns;
using BomberosAPI.Domain.Entities;
using BomberosAPI.Domain.Repositories;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Moq;
using Xunit;
using ValidationException = BomberosAPI.Application.Common.Exceptions.ValidationException;

namespace BomberosAPI.Application.UnitTests.VitalSigns.Services;

public class VitalSignsMeasurementServiceTests
{
    private readonly Mock<IVitalSignsMeasurementRepository> _mockRepo;
    private readonly Mock<ISessionParticipantRepository> _mockParticipantRepo;
    private readonly Mock<IHealthPersonnelRepository> _mockHpRepo;
    private readonly Mock<ITrainingSessionRepository> _mockSessionRepo;
    private readonly Mock<IValidator<CreateVitalSignsMeasurementRequest>> _mockValidator;
    private readonly VitalSignsMeasurementService _sut;

    public VitalSignsMeasurementServiceTests()
    {
        _mockRepo = new Mock<IVitalSignsMeasurementRepository>();
        _mockParticipantRepo = new Mock<ISessionParticipantRepository>();
        _mockHpRepo = new Mock<IHealthPersonnelRepository>();
        _mockSessionRepo = new Mock<ITrainingSessionRepository>();
        _mockValidator = new Mock<IValidator<CreateVitalSignsMeasurementRequest>>();

        _mockValidator
            .Setup(v => v.ValidateAsync(It.IsAny<CreateVitalSignsMeasurementRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _sut = new VitalSignsMeasurementService(
            _mockRepo.Object, _mockParticipantRepo.Object, _mockHpRepo.Object, _mockSessionRepo.Object, _mockValidator.Object);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsMappedDtos()
    {
        var items = new List<VitalSignsMeasurement>
        {
            new VitalSignsMeasurement { VitalSignsMeasurementId = Guid.NewGuid(), HeartRate = 80 }
        };
        _mockRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(items);

        var result = await _sut.GetAllAsync();

        result.Should().NotBeEmpty();
        result[0].HeartRate.Should().Be(80);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        var id = Guid.NewGuid();
        var v = new VitalSignsMeasurement { VitalSignsMeasurementId = id, HeartRate = 90 };
        _mockRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(v);

        var result = await _sut.GetByIdAsync(id);

        result.Should().NotBeNull();
        result.HeartRate.Should().Be(90);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ThrowsNotFoundException()
    {
        var id = Guid.NewGuid();
        _mockRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((VitalSignsMeasurement)null!);

        var act = async () => await _sut.GetByIdAsync(id);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_ReturnsDto()
    {
        var pId = Guid.NewGuid();
        var hpId = Guid.NewGuid();
        var request = new CreateVitalSignsMeasurementRequest(pId, hpId, 80, 120, 80, 36.5m, 98, "Portero", false, true);

        _mockParticipantRepo.Setup(r => r.GetByIdAsync(pId, It.IsAny<CancellationToken>())).ReturnsAsync(new SessionParticipant());
        _mockHpRepo.Setup(r => r.GetByIdAsync(hpId, It.IsAny<CancellationToken>())).ReturnsAsync(new BomberosAPI.Domain.Entities.HealthPersonnel());

        var result = await _sut.CreateAsync(request);

        result.Should().NotBeNull();
        result.HeartRate.Should().Be(80);
        result.PracticeRole.Should().Be("Portero");
        result.IsSmoker.Should().Be(false);
        result.ExposedToSmoke48h.Should().Be(true);
        _mockRepo.Verify(r => r.AddAsync(It.IsAny<VitalSignsMeasurement>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ParticipantNotFound_ThrowsNotFoundException()
    {
        var request = new CreateVitalSignsMeasurementRequest(Guid.NewGuid(), Guid.NewGuid(), 80, 120, 80, 36.5m, 98, "Portero", false, true);
        _mockParticipantRepo.Setup(r => r.GetByIdAsync(request.SessionParticipantId, It.IsAny<CancellationToken>())).ReturnsAsync((SessionParticipant)null!);

        var act = async () => await _sut.CreateAsync(request);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CreateAsync_HealthPersonnelNotFound_ThrowsNotFoundException()
    {
        var request = new CreateVitalSignsMeasurementRequest(Guid.NewGuid(), Guid.NewGuid(), 80, 120, 80, 36.5m, 98, "Portero", false, true);
        _mockParticipantRepo.Setup(r => r.GetByIdAsync(request.SessionParticipantId, It.IsAny<CancellationToken>())).ReturnsAsync(new SessionParticipant());
        _mockHpRepo.Setup(r => r.GetByIdAsync(request.RegisteredByHealthPersonnelId, It.IsAny<CancellationToken>())).ReturnsAsync((BomberosAPI.Domain.Entities.HealthPersonnel)null!);

        var act = async () => await _sut.CreateAsync(request);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CreateAsync_InvalidRequest_ThrowsValidationException()
    {
        var request = new CreateVitalSignsMeasurementRequest(Guid.NewGuid(), Guid.NewGuid(), 80, 120, 80, 36.5m, 98, "Portero", false, true);
        var validationResult = new ValidationResult(new[] { new ValidationFailure("HeartRate", "Invalid") });
        _mockValidator.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(validationResult);

        var act = async () => await _sut.CreateAsync(request);
        await act.Should().ThrowAsync<ValidationException>();
    }
}
