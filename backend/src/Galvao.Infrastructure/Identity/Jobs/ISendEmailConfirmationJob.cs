namespace Galvao.Infrastructure.Identity.Jobs;

public interface ISendEmailConfirmationJob
{
    Task SendConfirmationEmailAsync(
        Guid userId,
        string confirmationLink,
        CancellationToken cancellationToken = default);
}
