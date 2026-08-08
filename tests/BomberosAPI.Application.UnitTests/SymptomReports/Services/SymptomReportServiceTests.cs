using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BomberosAPI.Application.Common.Exceptions;
using BomberosAPI.Application.Features.SymptomReports;
using BomberosAPI.Domain.Entities;
using BomberosAPI.Domain.Repositories;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Moq;
using Xunit;
using ValidationException = BomberosAPI.Application.Common.Exceptions.ValidationException;

namespace BomberosAPI.Application.UnitTests.SymptomReports.Services;

public class SymptomReportServiceTests
{
    private readonly Mock<ISymptomReportRepository> _mockRepo;
    private readonly Mock<ISessionParticipantRepository> _mockParticipantRepo;
    private readonly Mock<IUserRepository> _mockUserRepo;
    private readonly Mock<IValidator<CreateSymptomReportRequest>> _mockValidator;
    private readonly SymptomReportService _sut;

    public SymptomReportServiceTests()
    {
        _mockRepo = new Mock<ISymptomReportRepository>();
        _mockParticipantRepo = new Mock<ISessionParticipantRepository>();
        _mockUserRepo = new Mock<IUserRepository>();
        _mockValidator = new Mock<IValidator<CreateSymptomReportRequest>>();

        _mockValidator
            .Setup(v => v.ValidateAsync(It.IsAny<CreateSymptomReportRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _sut = new SymptomReportService(
            _mockRepo.Object, _mockParticipantRepo.Object, _mockUserRepo.Object, _mockValidator.Object);
    }

    private static CreateSymptomReportRequest ValidRequest(Guid participantId, Guid userId) => new(
        participantId, userId, "Moderate", "Mareo y náuseas", true);

    [Fact]
    public async Task CreateAsync_ValidRequest_ReturnsDto()
    {
        var participantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _mockParticipantRepo.Setup(r => r.GetByIdAsync(participantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SessionParticipant { SessionParticipantId = participantId });
        _mockUserRepo.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { UserId = userId });

        var result = await _sut.CreateAsync(ValidRequest(participantId, userId));

        result.Should().NotBeNull();
        result.SessionParticipantId.Should().Be(participantId);
        result.RequiresAlert.Should().BeTrue();
        _mockRepo.Verify(r => r.AddAsync(It.IsAny<SymptomReport>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_InvalidRequest_ThrowsValidationException()
    {
        var request = ValidRequest(Guid.NewGuid(), Guid.NewGuid());
        var validationResult = new ValidationResult(new[] { new ValidationFailure("SessionParticipantId", "Required") });
        _mockValidator.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(validationResult);

        var act = async () => await _sut.CreateAsync(request);

        await act.Should().ThrowAsync<ValidationException>();
        _mockRepo.Verify(r => r.AddAsync(It.IsAny<SymptomReport>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_UnknownParticipant_ThrowsNotFoundException()
    {
        var participantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _mockParticipantRepo.Setup(r => r.GetByIdAsync(participantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SessionParticipant)null!);

        var act = async () => await _sut.CreateAsync(ValidRequest(participantId, userId));

        await act.Should().ThrowAsync<NotFoundException>();
        _mockRepo.Verify(r => r.AddAsync(It.IsAny<SymptomReport>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_UnknownReporter_ThrowsNotFoundException()
    {
        var participantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _mockParticipantRepo.Setup(r => r.GetByIdAsync(participantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SessionParticipant { SessionParticipantId = participantId });
        _mockUserRepo.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User)null!);

        var act = async () => await _sut.CreateAsync(ValidRequest(participantId, userId));

        await act.Should().ThrowAsync<NotFoundException>();
        _mockRepo.Verify(r => r.AddAsync(It.IsAny<SymptomReport>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ThrowsNotFoundException()
    {
        var id = Guid.NewGuid();
        _mockRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((SymptomReport)null!);

        var act = async () => await _sut.GetByIdAsync(id);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetByTraineeAsync_ReturnsMappedDtos()
    {
        var traineeId = Guid.NewGuid();
        var items = new List<SymptomReport>
        {
            new SymptomReport { SymptomReportId = Guid.NewGuid(), Severity = "Mild" },
        };
        _mockRepo.Setup(r => r.GetByTraineeAsync(traineeId, It.IsAny<CancellationToken>())).ReturnsAsync(items);

        var result = await _sut.GetByTraineeAsync(traineeId);

        result.Should().HaveCount(1);
        result[0].Severity.Should().Be("Mild");
    }
}
