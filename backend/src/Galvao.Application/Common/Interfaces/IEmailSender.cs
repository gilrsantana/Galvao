using Galvao.Domain.MemberContactAggregate.Enums;
using Galvao.Shared;

namespace Galvao.Application.Common.Interfaces;

public interface IEmailSender
{
    Task<Result> SendEmailAsync(
        List<string> to,
        string subject,
        string htmlContent,
        Guid? memberId = null,
        ETypeOfMessage? typeOfMessage = null,
        CancellationToken cancellationToken = default);
}
