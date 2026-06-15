using Galvao.Shared;

namespace Galvao.Application.Common.Interfaces;

public interface IEmailSender
{
    Task<Result> SendEmailAsync(string to, string subject, string htmlContent, CancellationToken cancellationToken = default);
}
