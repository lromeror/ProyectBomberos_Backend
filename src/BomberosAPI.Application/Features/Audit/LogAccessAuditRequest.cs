namespace BomberosAPI.Application.Features.Audit;

/// <summary>
/// Body sent by the frontend's `useAuditTrail` hook on every medical-screen mount.
/// `at` (client timestamp) is intentionally not accepted — audit timestamps are
/// always stamped server-side so they can't be spoofed by the client's clock.
/// </summary>
public record LogAccessAuditRequest(string? ResourceType, Guid? ResourceId, string? Action);
