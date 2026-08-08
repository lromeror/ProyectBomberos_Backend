using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BomberosAPI.Application.Common.Exceptions;
using BomberosAPI.Application.Common.Interfaces;
using BomberosAPI.Application.Features.Auth;
using BomberosAPI.Domain.Entities;
using BomberosAPI.Domain.Repositories;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Moq;
using Xunit;

namespace BomberosAPI.Application.UnitTests.Auth.Services;

public class AuthServiceTests
{
    private readonly Mock<IAuthRepository> _mockRepo;
    private readonly Mock<IPasswordHasher> _mockPasswordHasher;
    private readonly Mock<IJwtTokenService> _mockJwtService;
    private readonly Mock<IValidator<LoginRequest>> _mockValidator;
    private readonly Mock<IValidator<ForgotPasswordRequest>> _mockForgotValidator;
    private readonly Mock<IValidator<ResetPasswordRequest>> _mockResetValidator;
    private readonly Mock<IValidator<ChangePasswordRequest>> _mockChangePasswordValidator;
    private readonly Mock<IAuthSessionRepository> _mockAuthSessionRepo;
    private readonly AuthService _sut; // System Under Test

    public AuthServiceTests()
    {
        _mockRepo = new Mock<IAuthRepository>();
        _mockPasswordHasher = new Mock<IPasswordHasher>();
        _mockJwtService = new Mock<IJwtTokenService>();
        _mockValidator = new Mock<IValidator<LoginRequest>>();
        _mockForgotValidator = new Mock<IValidator<ForgotPasswordRequest>>();
        _mockResetValidator = new Mock<IValidator<ResetPasswordRequest>>();
        _mockChangePasswordValidator = new Mock<IValidator<ChangePasswordRequest>>();
        _mockAuthSessionRepo = new Mock<IAuthSessionRepository>();

        // Configuración por defecto: la validación del request pasa exitosamente
        _mockValidator
            .Setup(v => v.ValidateAsync(It.IsAny<LoginRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _sut = new AuthService(
            _mockRepo.Object,
            _mockPasswordHasher.Object,
            _mockJwtService.Object,
            _mockValidator.Object,
            _mockForgotValidator.Object,
            _mockResetValidator.Object,
            _mockChangePasswordValidator.Object,
            new AuthSessionService(_mockAuthSessionRepo.Object));
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsLoginResultWithToken()
    {
        // Arrange
        var request = new LoginRequest("test@bomberos.org", "password123");
        var user = new User
        {
            UserId = Guid.NewGuid(),
            Email = request.Email,
            AccountStatus = "active",
            FirstName = "John",
            LastName = "Doe"
        };
        var credential = new UserCredential
        {
            UserId = user.UserId,
            PasswordHash = "hashed_password123"
        };
        var roles = new List<string> { "trainee" };
        var expectedToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.mocked_token";
        var expectedExpiration = DateTime.UtcNow.AddHours(1);

        _mockRepo.Setup(r => r.FindUserByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        
        _mockRepo.Setup(r => r.FindCredentialByUserIdAsync(user.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential);
            
        _mockRepo.Setup(r => r.GetActiveRoleCodesByUserIdAsync(user.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(roles);

        _mockPasswordHasher.Setup(p => p.Verify(request.Password, credential.PasswordHash))
            .Returns(true);

        _mockJwtService.Setup(j => j.GenerateToken(user, roles))
            .Returns((expectedToken, expectedExpiration));

        // Act
        var result = await _sut.LoginAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Token.Should().Be(expectedToken);
        result.Email.Should().Be(user.Email);
        result.Roles.Should().BeEquivalentTo(roles);
    }

    [Fact]
    public async Task LoginAsync_UserNotFound_ThrowsUnauthorizedException()
    {
        // Arrange
        var request = new LoginRequest("notfound@bomberos.org", "password123");

        _mockRepo.Setup(r => r.FindUserByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User)null!);

        // Act
        var act = async () => await _sut.LoginAsync(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("Invalid credentials.");
    }

    [Fact]
    public async Task LoginAsync_InvalidPassword_ThrowsUnauthorizedException()
    {
        // Arrange
        var request = new LoginRequest("test@bomberos.org", "wrong_password");
        var user = new User { UserId = Guid.NewGuid(), Email = request.Email, AccountStatus = "active" };
        var credential = new UserCredential { UserId = user.UserId, PasswordHash = "hashed_password123" };

        _mockRepo.Setup(r => r.FindUserByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _mockRepo.Setup(r => r.FindCredentialByUserIdAsync(user.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential);

        _mockPasswordHasher.Setup(p => p.Verify(request.Password, credential.PasswordHash))
            .Returns(false); // Simulated wrong password

        // Act
        var act = async () => await _sut.LoginAsync(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("Invalid credentials.");
    }

    [Fact]
    public async Task LoginAsync_AccountInactive_ThrowsBusinessRuleException()
    {
        // Arrange
        var request = new LoginRequest("test@bomberos.org", "password123");
        var user = new User { UserId = Guid.NewGuid(), Email = request.Email, AccountStatus = "inactive" }; // Status invalid

        _mockRepo.Setup(r => r.FindUserByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var act = async () => await _sut.LoginAsync(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("Account is inactive. Contact your administrator.");
    }

    [Fact]
    public async Task LoginAsync_InvalidPassword_IncrementsFailedAttempts()
    {
        var request = new LoginRequest("test@bomberos.org", "wrong_password");
        var user = new User { UserId = Guid.NewGuid(), Email = request.Email, AccountStatus = "active" };
        var credential = new UserCredential { UserId = user.UserId, PasswordHash = "hashed", FailedAttempts = 2 };

        _mockRepo.Setup(r => r.FindUserByEmailAsync(request.Email, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _mockRepo.Setup(r => r.FindCredentialByUserIdAsync(user.UserId, It.IsAny<CancellationToken>())).ReturnsAsync(credential);
        _mockPasswordHasher.Setup(p => p.Verify(request.Password, credential.PasswordHash)).Returns(false);

        var act = async () => await _sut.LoginAsync(request, CancellationToken.None);
        await act.Should().ThrowAsync<UnauthorizedException>();

        _mockRepo.Verify(r => r.UpdateCredentialAsync(
            It.Is<UserCredential>(c => c.FailedAttempts == 3 && c.LockedUntil == null),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_ReachesMaxFailedAttempts_LocksAccountAndResetsCounter()
    {
        var request = new LoginRequest("test@bomberos.org", "wrong_password");
        var user = new User { UserId = Guid.NewGuid(), Email = request.Email, AccountStatus = "active" };
        var credential = new UserCredential { UserId = user.UserId, PasswordHash = "hashed", FailedAttempts = 4 };

        _mockRepo.Setup(r => r.FindUserByEmailAsync(request.Email, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _mockRepo.Setup(r => r.FindCredentialByUserIdAsync(user.UserId, It.IsAny<CancellationToken>())).ReturnsAsync(credential);
        _mockPasswordHasher.Setup(p => p.Verify(request.Password, credential.PasswordHash)).Returns(false);

        var act = async () => await _sut.LoginAsync(request, CancellationToken.None);
        await act.Should().ThrowAsync<UnauthorizedException>();

        _mockRepo.Verify(r => r.UpdateCredentialAsync(
            It.Is<UserCredential>(c => c.FailedAttempts == 0 && c.LockedUntil != null && c.LockedUntil > DateTime.UtcNow),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_AccountLocked_ThrowsBusinessRuleException_EvenWithCorrectPassword()
    {
        var request = new LoginRequest("test@bomberos.org", "password123");
        var user = new User { UserId = Guid.NewGuid(), Email = request.Email, AccountStatus = "active" };
        var credential = new UserCredential
        {
            UserId = user.UserId,
            PasswordHash = "hashed_password123",
            LockedUntil = DateTime.UtcNow.AddMinutes(10),
        };

        _mockRepo.Setup(r => r.FindUserByEmailAsync(request.Email, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _mockRepo.Setup(r => r.FindCredentialByUserIdAsync(user.UserId, It.IsAny<CancellationToken>())).ReturnsAsync(credential);
        // La contraseña sería correcta, pero ni siquiera debería llegar a verificarse.
        _mockPasswordHasher.Setup(p => p.Verify(request.Password, credential.PasswordHash)).Returns(true);

        var act = async () => await _sut.LoginAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleException>();
        _mockPasswordHasher.Verify(p => p.Verify(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ResetsPreviousFailedAttempts()
    {
        var request = new LoginRequest("test@bomberos.org", "password123");
        var user = new User { UserId = Guid.NewGuid(), Email = request.Email, AccountStatus = "active", FirstName = "John", LastName = "Doe" };
        var credential = new UserCredential { UserId = user.UserId, PasswordHash = "hashed_password123", FailedAttempts = 3 };
        var roles = new List<string> { "trainee" };

        _mockRepo.Setup(r => r.FindUserByEmailAsync(request.Email, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _mockRepo.Setup(r => r.FindCredentialByUserIdAsync(user.UserId, It.IsAny<CancellationToken>())).ReturnsAsync(credential);
        _mockRepo.Setup(r => r.GetActiveRoleCodesByUserIdAsync(user.UserId, It.IsAny<CancellationToken>())).ReturnsAsync(roles);
        _mockPasswordHasher.Setup(p => p.Verify(request.Password, credential.PasswordHash)).Returns(true);
        _mockJwtService.Setup(j => j.GenerateToken(user, roles)).Returns(("token", DateTime.UtcNow.AddHours(1)));

        await _sut.LoginAsync(request, CancellationToken.None);

        _mockRepo.Verify(r => r.UpdateCredentialAsync(
            It.Is<UserCredential>(c => c.FailedAttempts == 0 && c.LockedUntil == null),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
