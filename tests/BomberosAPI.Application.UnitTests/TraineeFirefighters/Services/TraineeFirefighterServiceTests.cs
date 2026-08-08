using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BomberosAPI.Application.Common.Exceptions;
using BomberosAPI.Application.Features.Audit;
using BomberosAPI.Application.Features.TraineeFirefighters;
using BomberosAPI.Domain.Entities;
using BomberosAPI.Domain.Repositories;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Moq;
using Xunit;

namespace BomberosAPI.Application.UnitTests.TraineeFirefighters.Services;

public class TraineeFirefighterServiceTests
{
    private readonly Mock<ITraineeFirefighterRepository> _mockRepo;
    private readonly Mock<IValidator<CreateTraineeFirefighterRequest>> _mockValidator;
    private readonly Mock<IChangeAuditRepository> _mockChangeAuditRepo;
    private readonly TraineeFirefighterService _sut;

    public TraineeFirefighterServiceTests()
    {
        _mockRepo = new Mock<ITraineeFirefighterRepository>();
        _mockValidator = new Mock<IValidator<CreateTraineeFirefighterRequest>>();
        _mockChangeAuditRepo = new Mock<IChangeAuditRepository>();

        // Por defecto, asumimos que el request pasa la validación FluentValidation
        _mockValidator
            .Setup(v => v.ValidateAsync(It.IsAny<CreateTraineeFirefighterRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _sut = new TraineeFirefighterService(_mockRepo.Object, _mockValidator.Object,
            new ChangeAuditService(_mockChangeAuditRepo.Object));
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_ReturnsDtoAndAddsToRepo()
    {
        // Arrange
        var request = new CreateTraineeFirefighterRequest(
            UserId: Guid.NewGuid(),
            ApplicantCode: "ASP-001",
            BirthDate: new DateOnly(2000, 1, 1),
            Sex: "M",
            BloodType: "O+",
            EmergencyContactName: "Jane Doe",
            EmergencyContactPhone: "555-1234"
        );

        _mockRepo.Setup(r => r.ExistsByApplicantCodeAsync(request.ApplicantCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false); // Code not in use

        _mockRepo.Setup(r => r.AddAsync(It.IsAny<TraineeFirefighter>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.CreateAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ApplicantCode.Should().Be(request.ApplicantCode);
        result.BloodType.Should().Be(request.BloodType);
        result.TrainingStatus.Should().Be("Active");
        result.TraineeFirefighterId.Should().NotBeEmpty();

        _mockRepo.Verify(r => r.AddAsync(It.Is<TraineeFirefighter>(t => 
            t.ApplicantCode == request.ApplicantCode && 
            t.BloodType == request.BloodType), 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_DuplicateApplicantCode_ThrowsConflictException()
    {
        // Arrange
        var request = new CreateTraineeFirefighterRequest(
            UserId: Guid.NewGuid(), ApplicantCode: "ASP-001", 
            BirthDate: DateOnly.FromDateTime(DateTime.Now), Sex: "M", BloodType: "O+", 
            EmergencyContactName: "Jane", EmergencyContactPhone: "123");

        _mockRepo.Setup(r => r.ExistsByApplicantCodeAsync(request.ApplicantCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true); // SIMULATING CONFLICT

        // Act
        var act = async () => await _sut.CreateAsync(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("Applicant code already in use.");
            
        _mockRepo.Verify(r => r.AddAsync(It.IsAny<TraineeFirefighter>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ExistingId_UpdatesFieldsAndReturnsDto()
    {
        // Arrange
        var traineeId = Guid.NewGuid();
        var existingTrainee = new TraineeFirefighter
        {
            TraineeFirefighterId = traineeId,
            ApplicantCode = "ASP-002",
            BloodType = "A-",
            EmergencyContactName = "Old Contact"
        };
        
        var request = new UpdateTraineeFirefighterRequest("O+", "New Contact", "999-9999");

        _mockRepo.Setup(r => r.GetByIdAsync(traineeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTrainee);
            
        _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<TraineeFirefighter>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.UpdateAsync(traineeId, request, CancellationToken.None);

        // Assert
        result.BloodType.Should().Be("O+");
        result.EmergencyContactName.Should().Be("New Contact");
        
        _mockRepo.Verify(r => r.UpdateAsync(It.Is<TraineeFirefighter>(t => 
            t.BloodType == "O+" && t.EmergencyContactName == "New Contact"), 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingId_ThrowsNotFoundException()
    {
        // Arrange
        var traineeId = Guid.NewGuid();
        var request = new UpdateTraineeFirefighterRequest("O+", "Name", "123");

        _mockRepo.Setup(r => r.GetByIdAsync(traineeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TraineeFirefighter)null!);

        // Act
        var act = async () => await _sut.UpdateAsync(traineeId, request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }
}
