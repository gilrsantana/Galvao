using Galvao.Application.Common.CQRS;

namespace Galvao.Application.UseCases.Members.Commands;

public record UpdateMarketingPreferencesCommand(
    Guid MemberId,
    bool AcceptNews,
    bool AcceptPromo,
    string? ConsentToken = null,
    DateTime? ConsentedAt = null,
    string? IpAddress = null,
    string? Source = null) : ICommand;


