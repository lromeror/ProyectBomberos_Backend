using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BomberosAPI.Application.Common.Exceptions;
using BomberosAPI.Application.Features.Audit;
using BomberosAPI.Application.Features.MedicalHistory;
using BomberosAPI.Domain.Entities;
using BomberosAPI.Domain.Repositories;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Moq;
using Xunit;
using ValidationException = BomberosAPI.Application.Common.Exceptions.ValidationException;
using MedicalHistoryEntity = BomberosAPI.Domain.Entities.MedicalHistory;

namespace BomberosAPI.Application.UnitTests.MedicalHistory.Services;

public class MedicalHistoryServiceTests
{
    private readonly Mock<IMedicalHistoryRepository> _mockRepo;
    private readonly Mock<ITraineeFirefighterRepository> _mockTraineeRepo;
    private readonly Mock<IHealthPersonnelRepository> _mockHpRepo;
    private readonly Mock<IValidator<CreateMedicalHistoryRequest>> _mockValidator;
    private readonly Mock<IChangeAuditRepository> _mockChangeAuditRepo;
    private readonly MedicalHistoryService _sut;

    public MedicalHistoryServiceTests()
    {
        _mockRepo = new Mock<IMedicalHistoryRepository>();
        _mockTraineeRepo = new Mock<ITraineeFirefighterRepository>();
        _mockHpRepo = new Mock<IHealthPersonnelRepository>();
        _mockValidator = new Mock<IValidator<CreateMedicalHistoryRequest>>();
        _mockChangeAuditRepo = new Mock<IChangeAuditRepository>();

        _mockValidator
            .Setup(v => v.ValidateAsync(It.IsAny<CreateMedicalHistoryRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _sut = new MedicalHistoryService(
            _mockRepo.Object, _mockTraineeRepo.Object, _mockHpRepo.Object, _mockValidator.Object,
            new ChangeAuditService(_mockChangeAuditRepo.Object));
    }

    [Fact]
    public async Task GetAllAsync_ReturnsMappedDtos()
    {
        var items = new List<MedicalHistoryEntity>
        {
            new MedicalHistoryEntity { MedicalHistoryId = Guid.NewGuid(), Allergies = "None" }
        };
        _mockRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(items);

        var result = await _sut.GetAllAsync();

        result.Should().NotBeEmpty();
        result[0].Allergies.Should().Be("None");
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        var id = Guid.NewGuid();
        var mh = new MedicalHistoryEntity { MedicalHistoryId = id, Allergies = "None" };
        _mockRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(mh);

        var result = await _sut.GetByIdAsync(id);

        result.Should().NotBeNull();
        result.Allergies.Should().Be("None");
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ThrowsNotFoundException()
    {
        var id = Guid.NewGuid();
        _mockRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((MedicalHistoryEntity)null!);

        var act = async () => await _sut.GetByIdAsync(id);
        await act.Should().ThrowAsync<NotFoundException>();
    }
    
    [Fact]
    public async Task GetByTraineeAsync_Existing_ReturnsDto()
    {
        var id = Guid.NewGuid();
        var mh = new MedicalHistoryEntity { MedicalHistoryId = Guid.NewGuid(), Allergies = "Dust", TraineeFirefighterId = id };
        _mockRepo.Setup(r => r.GetByTraineeAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(mh);

        var result = await _sut.GetByTraineeAsync(id);

        result.Should().NotBeNull();
        result.Allergies.Should().Be("Dust");
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_ReturnsDto()
    {
        var traineeId = Guid.NewGuid();
        var hpId = Guid.NewGuid();
        var request = new CreateMedicalHistoryRequest(traineeId, hpId, "None", "None", "None", "None");

        _mockTraineeRepo.Setup(r => r.GetByIdAsync(traineeId, It.IsAny<CancellationToken>())).ReturnsAsync(new TraineeFirefighter());
        _mockHpRepo.Setup(r => r.GetByIdAsync(hpId, It.IsAny<CancellationToken>())).ReturnsAsync(new BomberosAPI.Domain.Entities.HealthPersonnel());
        _mockRepo.Setup(r => r.ExistsByTraineeAsync(traineeId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await _sut.CreateAsync(request, Guid.NewGuid());

        result.Should().NotBeNull();
        result.Allergies.Should().Be("None");
        _mockRepo.Verify(r => r.AddAsync(It.IsAny<MedicalHistoryEntity>(), It.IsAny<CancellationToken>()), Times.Once);
    }
    
    [Fact]
    public async Task CreateAsync_InvalidRequest_ThrowsValidationException()
    {
        var request = new CreateMedicalHistoryRequest(Guid.NewGuid(), Guid.NewGuid(), "None", "None", "None", "None");
        var validationResult = new ValidationResult(new[] { new ValidationFailure("TraineeFirefighterId", "Required") });
        _mockValidator.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(validationResult);

        var act = async () => await _sut.CreateAsync(request, Guid.NewGuid());

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task CreateAsync_TraineeNotFound_ThrowsNotFoundException()
    {
        var request = new CreateMedicalHistoryRequest(Guid.NewGuid(), Guid.NewGuid(), "None", "None", "None", "None");
        _mockTraineeRepo.Setup(r => r.GetByIdAsync(request.TraineeFirefighterId, It.IsAny<CancellationToken>())).ReturnsAsync((TraineeFirefighter)null!);

        var act = async () => await _sut.CreateAsync(request, Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CreateAsync_MedicalHistoryAlreadyExists_ThrowsConflictException()
    {
        var traineeId = Guid.NewGuid();
        var request = new CreateMedicalHistoryRequest(traineeId, Guid.NewGuid(), "None", "None", "None", "None");
        _mockTraineeRepo.Setup(r => r.GetByIdAsync(traineeId, It.IsAny<CancellationToken>())).ReturnsAsync(new TraineeFirefighter());
        _mockHpRepo.Setup(r => r.GetByIdAsync(request.CreatedByHealthPersonnelId, It.IsAny<CancellationToken>())).ReturnsAsync(new BomberosAPI.Domain.Entities.HealthPersonnel());
        _mockRepo.Setup(r => r.ExistsByTraineeAsync(traineeId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var act = async () => await _sut.CreateAsync(request, Guid.NewGuid());

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task UpdateAsync_ExistingId_UpdatesAndReturnsDto()
    {
        var id = Guid.NewGuid();
        var mh = new MedicalHistoryEntity { MedicalHistoryId = id, Allergies = "Old" };
        var request = new UpdateMedicalHistoryRequest("New", "New", "New", "New");

        _mockRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(mh);

        var result = await _sut.UpdateAsync(id, request, Guid.NewGuid());

        result.Should().NotBeNull();
        result.Allergies.Should().Be("New");
        _mockRepo.Verify(r => r.UpdateAsync(It.IsAny<MedicalHistoryEntity>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
