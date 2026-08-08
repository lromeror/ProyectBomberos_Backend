using BomberosAPI.Domain.Entities;
using BomberosAPI.Domain.Repositories;
using BomberosAPI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BomberosAPI.Infrastructure.Repositories;

public class ConsentDocumentRepository : IConsentDocumentRepository
{
    private readonly AppDbContext _db;

    public ConsentDocumentRepository(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<ConsentDocument>> GetActiveAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        return await _db.ConsentDocuments
            .AsNoTracking()
            .Where(d => d.ValidFrom <= now && (d.ValidUntil == null || d.ValidUntil > now))
            .OrderByDescending(d => d.ValidFrom)
            .ToListAsync(ct);
    }

    public Task<ConsentDocument?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.ConsentDocuments.FirstOrDefaultAsync(d => d.ConsentDocumentId == id, ct);

    public async Task AddAsync(ConsentDocument document, CancellationToken ct = default)
    {
        _db.ConsentDocuments.Add(document);
        await _db.SaveChangesAsync(ct);
    }
}
