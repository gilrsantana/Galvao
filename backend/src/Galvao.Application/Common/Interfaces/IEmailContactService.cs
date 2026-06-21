using Galvao.Shared;

namespace Galvao.Application.Common.Interfaces;

public interface IEmailContactService
{
    Task<Result<string>> CreateContactAsync(
        string email,
        string firstName,
        string lastName,
        bool acceptNews,
        bool acceptPromo,
        CancellationToken cancellationToken = default);

    Task<Result> UpdateContactAsync(
        string externalContactId,
        string firstName,
        string lastName,
        bool unsubscribed,
        CancellationToken cancellationToken = default);

    Task<Result> DeleteContactAsync(
        string externalContactId,
        CancellationToken cancellationToken = default);
}
