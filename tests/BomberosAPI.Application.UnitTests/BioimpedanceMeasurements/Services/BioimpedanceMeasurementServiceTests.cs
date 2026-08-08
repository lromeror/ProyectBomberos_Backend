using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BomberosAPI.Application.Common.Exceptions;
using BomberosAPI.Application.Features.BioimpedanceMeasurements;
using BomberosAPI.Domain.Entities;
using BomberosAPI.Domain.Repositories;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Moq;
using Xunit;
using ValidationException = BomberosAPI.Application.Common.Exceptions.ValidationException;

namespace BomberosAPI.Application.UnitTests.BioimpedanceMeasurements.Services;

public class BioimpedanceMeasurementServiceTests
{
    private readonly Mock<IBioimpedanceMeasurementRepository> _mockRepo;
    private readonly Mock<ISessionParticipantRepository> _mockParticipantRepo;
    private readonly Mock<IHealthPersonnelRepository> _mockHpRepo;
    private readonly Mock<IValidator<CreateBioimpedanceMeasurementRequest>> _mockValidator;
    private readonly BioimpedanceMeasurementService _sut;

    public BioimpedanceMeasurementServiceTests()
    {
        _mockRepo = new Mock<IBioimpedanceMeasurementRepository>();
        _mockParticipantRepo = new Mock<ISessionParticipantRepository>();
        _mockHpRepo = new Mock<IHealthPersonnelRepository>();
        _mockValidator = new Mock<IValidator<CreateBioimpedanceMeasurementRequest>>();

        _mockValidator
            .Setup(v => v.ValidateAsync(It.IsAny<CreateBioimpedanceMeasurementRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _sut = new BioimpedanceMeasurementService(
            _mockRepo.Object, _mockParticipantRepo.Object, _mockHpRepo.Object, _mockValidator.Object);
    }

    private static CreateBioimpedanceMeasurementRequest ValidRequest(Guid participantId, Guid hpId) => new(
        participantId, hpId, 80m, 18.5m, 35m, 55m, 1800m, 32m, 1.2m, 4.5m, 20m, 1);

    [Fact]
    public async Task CreateAsync_ValidRequest_ReturnsDto()
    {
        var participantId = Guid.NewGuid();
        var hpId = Guid.NewGuid();
        _mockParticipantRepo.Setup(r => r.GetByIdAsync(participantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SessionParticipant { SessionParticipantId = participantId });
        _mockHpRepo.Setup(r => r.GetByIdAsync(hpId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Domain.Entities.HealthPersonnel { HealthPersonnelId = hpId });

        var result = await _sut.CreateAsync(ValidRequest(participantId, hpId));

        result.Should().NotBeNull();
        result.SessionParticipantId.Should().Be(participantId);
        result.MetabolicAgeYears.Should().Be(32m);
        result.StroopErrors.Should().Be(1);
        _mockRepo.Verify(r => r.AddAsync(It.IsAny<Domain.Entities.BioimpedanceMeasurement>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_InvalidRequest_ThrowsValidationException()
    {
        var request = ValidRequest(Guid.NewGuid(), Guid.NewGuid());
        var validationResult = new ValidationResult(new[] { new ValidationFailure("SessionParticipantId", "Required") });
        _mockValidator.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(validationResult);

        var act = async () => await _sut.CreateAsync(request);

        await act.Should().ThrowAsync<ValidationException>();
        _mockRepo.Verify(r => r.AddAsync(It.IsAny<Domain.Entities.BioimpedanceMeasurement>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_UnknownParticipant_ThrowsNotFoundException()
    {
        var participantId = Guid.NewGuid();
        var hpId = Guid.NewGuid();
        _mockParticipantRepo.Setup(r => r.GetByIdAsync(participantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SessionParticipant)null!);

        var act = async () => await _sut.CreateAsync(ValidRequest(participantId, hpId));

        await act.Should().ThrowAsync<NotFoundException>();
        _mockRepo.Verify(r => r.AddAsync(It.IsAny<Domain.Entities.BioimpedanceMeasurement>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_UnknownHealthPersonnel_ThrowsNotFoundException()
    {
        var participantId = Guid.NewGuid();
        var hpId = Guid.NewGuid();
        _mockParticipantRepo.Setup(r => r.GetByIdAsync(participantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SessionParticipant { SessionParticipantId = participantId });
        _mockHpRepo.Setup(r => r.GetByIdAsync(hpId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Domain.Entities.HealthPersonnel)null!);

        var act = async () => await _sut.CreateAsync(ValidRequest(participantId, hpId));

        await act.Should().ThrowAsync<NotFoundException>();
        _mockRepo.Verify(r => r.AddAsync(It.IsAny<Domain.Entities.BioimpedanceMeasurement>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        var id = Guid.NewGuid();
        var measurement = new Domain.Entities.BioimpedanceMeasurement { BioimpedanceMeasurementId = id, WeightKg = 75m };
        _mockRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(measurement);

        var result = await _sut.GetByIdAsync(id);

        result.BioimpedanceMeasurementId.Should().Be(id);
        result.WeightKg.Should().Be(75m);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ThrowsNotFoundException()
    {
        var id = Guid.NewGuid();
        _mockRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((Domain.Entities.BioimpedanceMeasurement)null!);

        var act = async () => await _sut.GetByIdAsync(id);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetByTraineeAsync_ReturnsMappedDtos()
    {
        var traineeId = Guid.NewGuid();
        var items = new List<Domain.Entities.BioimpedanceMeasurement>
        {
            new Domain.Entities.BioimpedanceMeasurement { BioimpedanceMeasurementId = Guid.NewGuid(), LactatePreMmol = 1.1m },
        };
        _mockRepo.Setup(r => r.GetByTraineeAsync(traineeId, It.IsAny<CancellationToken>())).ReturnsAsync(items);

        var result = await _sut.GetByTraineeAsync(traineeId);

        result.Should().HaveCount(1);
        result[0].LactatePreMmol.Should().Be(1.1m);
    }
}
