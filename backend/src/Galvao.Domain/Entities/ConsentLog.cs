using Galvao.Shared;

namespace Galvao.Domain.Entities;

public class ConsentLog : BaseEntity
{
    public Guid MemberId { get; private set; }
    public string Action { get; private set; }
    public string? IpAddress { get; private set; }
    public string? Source { get; private set; }
    public string? ConsentToken { get; private set; }

    // EF Core Constructor
    private ConsentLog() : base()
    {
        Action = string.Empty;
    }

    // Parameterized Constructor
    private ConsentLog(Guid memberId, string action, string? ipAddress, string? source, string? consentToken) : base()
    {
        MemberId = memberId;
        Action = action;
        IpAddress = ipAddress;
        Source = source;
        ConsentToken = consentToken;
    }

    // Static Factory
    public static Result<ConsentLog> Create(Guid memberId, string action, string? ipAddress, string? source, string? consentToken)
    {
        if (memberId == Guid.Empty)
            return Result.Failure<ConsentLog>(new Error("ConsentLog.MemberIdRequired", "Member ID is required."));

        if (string.IsNullOrWhiteSpace(action))
            return Result.Failure<ConsentLog>(new Error("ConsentLog.ActionRequired", "Action is required."));

        if (action != "Opt-In" && action != "Opt-Out")
            return Result.Failure<ConsentLog>(new Error("ConsentLog.InvalidAction", "Action must be either 'Opt-In' or 'Opt-Out'."));

        return new ConsentLog(memberId, action, ipAddress, source, consentToken);
    }
}
