using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BomberosAPI.Application.Common.Exceptions;
using BomberosAPI.Application.Features.Audit;
using BomberosAPI.Application.Features.Auth;
using BomberosAPI.Application.Features.Users;
using BomberosAPI.Domain.Entities;
using BomberosAPI.Domain.Repositories;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BomberosAPI.Application.UnitTests.Users.Services;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _mockRepo;
    private readonly Mock<IRoleRepository> _mockRoleRepo;
    private readonly Mock<IUserRoleRepository> _mockUserRoleRepo;
    private readonly Mock<IValidator<CreateUserRequest>> _mockValidator;
    private readonly Mock<IChangeAuditRepository> _mockChangeAuditRepo;
    private readonly Mock<IRegistrationService> _mockRegistrationService;
    private readonly Mock<ILogger<UserService>> _mockLogger;
    private readonly UserService _sut;

    public UserServiceTests()
    {
        _mockRepo = new Mock<IUserRepository>();
        _mockRoleRepo = new Mock<IRoleRepository>();
        _mockUserRoleRepo = new Mock<IUserRoleRepository>();
        _mockValidator = new Mock<IValidator<CreateUserRequest>>();
        _mockChangeAuditRepo = new Mock<IChangeAuditRepository>();
        _mockRegistrationService = new Mock<IRegistrationService>();
        _mockLogger = new Mock<ILogger<UserService>>();

        _mockValidator
            .Setup(v => v.ValidateAsync(It.IsAny<CreateUserRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _sut = new UserService(_mockRepo.Object, _mockRoleRepo.Object, _mockUserRoleRepo.Object, _mockValidator.Object,
            new ChangeAuditService(_mockChangeAuditRepo.Object), _mockRegistrationService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsMappedDtos()
    {
        var users = new List<User> { new User { UserId = Guid.NewGuid(), Email = "test@test.com" } };
        _mockRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(users);
        
        var result = await _sut.GetAllAsync();
        
        result.Should().NotBeEmpty();
        result[0].Email.Should().Be("test@test.com");
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_ReturnsDto()
    {
        var request = new CreateUserRequest(Guid.NewGuid(), "test@test.com", "John", "Doe", "123456");
        _mockRepo.Setup(r => r.ExistsByEmailAsync(request.Email, It.IsAny<CancellationToken>())).ReturnsAsync(false);
            
        var result = await _sut.CreateAsync(request);
        
        result.Should().NotBeNull();
        result.Email.Should().Be(request.Email);
        _mockRepo.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_DuplicateEmail_ThrowsConflictException()
    {
        var request = new CreateUserRequest(Guid.NewGuid(), "test@test.com", "John", "Doe", "123456");
        _mockRepo.Setup(r => r.ExistsByEmailAsync(request.Email, It.IsAny<CancellationToken>())).ReturnsAsync(true);
            
        var act = async () => await _sut.CreateAsync(request);
        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task UpdateAsync_ExistingId_ReturnsUpdatedDto()
    {
        var id = Guid.NewGuid();
        var user = new User { UserId = id, Email = "test@test.com" };
        var request = new UpdateUserRequest("Jane", "Smith", "654321", "test@test.com");
        
        _mockRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        
        var result = await _sut.UpdateAsync(id, request);
        
        result.Should().NotBeNull();
        result.FirstName.Should().Be("Jane");
        _mockRepo.Verify(r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
    }
    
    [Fact]
    public async Task SetStatusAsync_ExistingId_UpdatesStatus()
    {
        var id = Guid.NewGuid();
        var user = new User { UserId = id, AccountStatus = "active" };
        
        _mockRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        await _sut.SetStatusAsync(id, "inactive", Guid.NewGuid());
        
        user.AccountStatus.Should().Be("inactive");
        _mockRepo.Verify(r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
