using BomberosAPI.Domain.Entities;
using BomberosAPI.Domain.Repositories;
using BomberosAPI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BomberosAPI.Infrastructure.Repositories;

public class UserRoleRepository : IUserRoleRepository
{
    private readonly AppDbContext _db;

    public UserRoleRepository(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<UserRole>> GetByUserIdAsync(Guid userId, CancellationToken ct = default) =>
        await _db.UserRoles.Where(ur => ur.UserId == userId).ToListAsync(ct);

    public async Task<IReadOnlyList<string>> GetActiveRoleCodesByUserIdAsync(Guid userId, CancellationToken ct = default) =>
        await (from ur in _db.UserRoles
               join r in _db.Roles on ur.RoleId equals r.RoleId
               where ur.UserId == userId && ur.IsActive
               select r.Code)
              .AsNoTracking()
              .ToListAsync(ct);

    public async Task AddAsync(UserRole userRole, CancellationToken ct = default)
    {
        _db.UserRoles.Add(userRole);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(UserRole userRole, CancellationToken ct = default)
    {
        _db.UserRoles.Update(userRole);
        await _db.SaveChangesAsync(ct);
    }
}
