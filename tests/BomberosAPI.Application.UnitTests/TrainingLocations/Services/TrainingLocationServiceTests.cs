using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BomberosAPI.Application.Common.Exceptions;
using BomberosAPI.Application.Features.TrainingLocations;
using BomberosAPI.Domain.Entities;
using BomberosAPI.Domain.Repositories;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Moq;
using Xunit;
using ValidationException = BomberosAPI.Application.Common.Exceptions.ValidationException;

namespace BomberosAPI.Application.UnitTests.TrainingLocations.Services;

public class TrainingLocationServiceTests
{
    private readonly Mock<ITrainingLocationRepository> _mockRepo;
    private readonly Mock<ITrainingInstitutionRepository> _mockInstitutionRepo;
    private readonly Mock<IValidator<CreateTrainingLocationRequest>> _mockValidator;
    private readonly TrainingLocationService _sut;

    public TrainingLocationServiceTests()
    {
        _mockRepo = new Mock<ITrainingLocationRepository>();
        _mockInstitutionRepo = new Mock<ITrainingInstitutionRepository>();
        _mockValidator = new Mock<IValidator<CreateTrainingLocationRequest>>();

        // Valid by default
        _mockValidator
            .Setup(v => v.ValidateAsync(It.IsAny<CreateTrainingLocationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _sut = new TrainingLocationService(_mockRepo.Object, _mockInstitutionRepo.Object, _mockValidator.Object);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsMappedDtos()
    {
        var locations = new List<TrainingLocation>
        {
            new TrainingLocation { TrainingLocationId = Guid.NewGuid(), Name = "Loc 1", LocationType = "Indoor" }
        };
        _mockRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(locations);

        var result = await _sut.GetAllAsync();

        result.Should().NotBeEmpty();
        result[0].Name.Should().Be("Loc 1");
        result[0].LocationType.Should().Be("Indoor");
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        var id = Guid.NewGuid();
        var location = new TrainingLocation { TrainingLocationId = id, Name = "Loc 1" };
        _mockRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(location);

        var result = await _sut.GetByIdAsync(id);

        result.Should().NotBeNull();
        result.TrainingLocationId.Should().Be(id);
        result.Name.Should().Be("Loc 1");
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ThrowsNotFoundException()
    {
        var id = Guid.NewGuid();
        _mockRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((TrainingLocation)null!);

        var act = async () => await _sut.GetByIdAsync(id);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_ReturnsDto()
    {
        var institutionId = Guid.NewGuid();
        var request = new CreateTrainingLocationRequest(institutionId, "New Location", "Indoor", "Address", 30);
        _mockInstitutionRepo.Setup(r => r.GetByIdAsync(institutionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TrainingInstitution { InstitutionId = institutionId, Name = "Inst" });

        var result = await _sut.CreateAsync(request);

        result.Should().NotBeNull();
        result.Name.Should().Be("New Location");
        result.InstitutionId.Should().Be(institutionId);
        _mockRepo.Verify(r => r.AddAsync(It.IsAny<TrainingLocation>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_InvalidRequest_ThrowsValidationException()
    {
        var request = new CreateTrainingLocationRequest(Guid.NewGuid(), "", null, null, null);
        var validationResult = new ValidationResult(new[] { new ValidationFailure("Name", "Name is required") });
        _mockValidator.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(validationResult);

        var act = async () => await _sut.CreateAsync(request);

        await act.Should().ThrowAsync<ValidationException>();
        _mockRepo.Verify(r => r.AddAsync(It.IsAny<TrainingLocation>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_UnknownInstitution_ThrowsNotFoundException()
    {
        var institutionId = Guid.NewGuid();
        var request = new CreateTrainingLocationRequest(institutionId, "New Location", null, null, null);
        _mockInstitutionRepo.Setup(r => r.GetByIdAsync(institutionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TrainingInstitution)null!);

        var act = async () => await _sut.CreateAsync(request);

        await act.Should().ThrowAsync<NotFoundException>();
        _mockRepo.Verify(r => r.AddAsync(It.IsAny<TrainingLocation>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ExistingId_UpdatesAndReturnsDto()
    {
        var id = Guid.NewGuid();
        var location = new TrainingLocation { TrainingLocationId = id, Name = "Old Name" };
        var request = new UpdateTrainingLocationRequest("New Name", "Outdoor", "New Address", 50);

        _mockRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(location);

        var result = await _sut.UpdateAsync(id, request);

        result.Should().NotBeNull();
        result.Name.Should().Be("New Name");
        _mockRepo.Verify(r => r.UpdateAsync(It.Is<TrainingLocation>(l => l.Name == "New Name"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingId_ThrowsNotFoundException()
    {
        var id = Guid.NewGuid();
        var request = new UpdateTrainingLocationRequest("New Name", null, null, null);
        _mockRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((TrainingLocation)null!);

        var act = async () => await _sut.UpdateAsync(id, request);
        await act.Should().ThrowAsync<NotFoundException>();
    }
}
