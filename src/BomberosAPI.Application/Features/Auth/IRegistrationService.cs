namespace BomberosAPI.Application.Features.Auth;

public interface IRegistrationService
{
    Task SendActivationEmailAsync(Guid userId, string email, string firstName, CancellationToken ct = default);
    Task ActivateAccountAsync(ActivateAccountRequest request, CancellationToken ct = default);
    Task<InvitationPreviewResult> PreviewInvitationAsync(string token, CancellationToken ct = default);
    Task CompleteRegistrationAsync(CompleteRegistrationRequest request, CancellationToken ct = default);
}
