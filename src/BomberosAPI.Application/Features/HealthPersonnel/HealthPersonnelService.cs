using BomberosAPI.Application.Common.Exceptions;
using BomberosAPI.Domain.Repositories;
using FluentValidation;
using HealthPersonnelEntity = BomberosAPI.Domain.Entities.HealthPersonnel;
using AppValidationException = BomberosAPI.Application.Common.Exceptions.ValidationException;

namespace BomberosAPI.Application.Features.HealthPersonnel;

public class HealthPersonnelService
{
    private readonly IHealthPersonnelRepository _repo;
    private readonly IUserRepository _userRepo;
    private readonly IValidator<CreateHealthPersonnelRequest> _createValidator;

    public HealthPersonnelService(
        IHealthPersonnelRepository repo,
        IUserRepository userRepo,
        IValidator<CreateHealthPersonnelRequest> createValidator)
    {
        _repo = repo;
        _userRepo = userRepo;
        _createValidator = createValidator;
    }

    public async Task<IReadOnlyList<HealthPersonnelDto>> GetAllAsync(CancellationToken ct = default)
    {
        var items = await _repo.GetAllAsync(ct);
        return items.Select(ToDto).ToList();
    }

    public async Task<HealthPersonnelDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var hp = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("HealthPersonnel", id);
        return ToDto(hp);
    }

    public async Task<HealthPersonnelDto> CreateAsync(CreateHealthPersonnelRequest request, CancellationToken ct = default)
    {
        var validation = await _createValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            var errors = validation.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            throw new AppValidationException(errors);
        }

        if (await _userRepo.GetByIdAsync(request.UserId, ct) is null)
            throw new NotFoundException("User", request.UserId);

        if (await _repo.GetByUserIdAsync(request.UserId, ct) is not null)
            throw new ConflictException("HealthPersonnel profile already exists for this user.");

        var hp = new HealthPersonnelEntity
        {
            HealthPersonnelId = Guid.NewGuid(),
            UserId = request.UserId,
            Profession = request.Profession,
            Specialty = request.Specialty,
            LicenseNumber = request.LicenseNumber,
            CanApproveDischarges = request.CanApproveDischarges
        };

        await _repo.AddAsync(hp, ct);
        return ToDto(hp);
    }

    public async Task<HealthPersonnelDto> UpdateAsync(Guid id, UpdateHealthPersonnelRequest request, CancellationToken ct = default)
    {
        var hp = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("HealthPersonnel", id);

        hp.Profession = request.Profession;
        hp.Specialty = request.Specialty;
        hp.LicenseNumber = request.LicenseNumber;
        hp.CanApproveDischarges = request.CanApproveDischarges;

        await _repo.UpdateAsync(hp, ct);
        return ToDto(hp);
    }

    /// <summary>Activa/desactiva (borrado lógico) un perfil de personal médico.</summary>
    public async Task SetActiveAsync(Guid id, bool isActive, CancellationToken ct = default)
    {
        var hp = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("HealthPersonnel", id);

        hp.IsActive = isActive;
        await _repo.UpdateAsync(hp, ct);
    }

    private static HealthPersonnelDto ToDto(HealthPersonnelEntity h) => new(
        h.HealthPersonnelId, h.UserId,
        h.User?.FirstName ?? "", h.User?.LastName ?? "", h.User?.Email ?? "", h.User?.Phone,
        h.Profession, h.Specialty, h.LicenseNumber, h.CanApproveDischarges, h.IsActive);
}