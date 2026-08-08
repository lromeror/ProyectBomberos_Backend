namespace BomberosAPI.Domain.Entities;

public class AccessAudit
{
    public Guid AccessAuditId { get; set; }
    public Guid UserId { get; set; }
    public Guid? AuthSessionId { get; set; }
    public string Event { get; set; } = null!;

    // Qué recurso se accedió (ej. "MEDICAL_RECORD" / id del registro). Nulo para
    // eventos de autenticación (login/logout) que no apuntan a un recurso específico.
    public string? ResourceType { get; set; }
    public Guid? ResourceId { get; set; }

    public string? Ip { get; set; }
    public string? UserAgent { get; set; }
    public bool Success { get; set; }
    public DateTime OccurredAt { get; set; }
}
