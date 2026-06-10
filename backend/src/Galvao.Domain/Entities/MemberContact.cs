using Galvao.Shared;

namespace Galvao.Domain.Entities;

public class MemberContact : BaseEntity
{
    public string ExternalContactId { get; private set; }
    public string Email { get; private set; }
    public bool Unsubscribed { get; private set; }

    // EF Core Constructor
    private MemberContact() : base()
    {
        ExternalContactId = string.Empty;
        Email = string.Empty;
    }

    private MemberContact(Guid memberId, string externalContactId, string email, bool unsubscribed) : base(memberId)
    {
        ExternalContactId = externalContactId;
        Email = email;
        Unsubscribed = unsubscribed;
    }

    public static Result<MemberContact> Create(Guid memberId, string externalContactId, string email, bool unsubscribed)
    {
        if (memberId == Guid.Empty)
            return Result.Failure<MemberContact>(new Error("MemberContact.InvalidMemberId", "Member ID cannot be empty."));

        if (string.IsNullOrWhiteSpace(externalContactId))
            return Result.Failure<MemberContact>(new Error("MemberContact.InvalidExternalContactId", "External Contact ID cannot be empty."));

        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            return Result.Failure<MemberContact>(new Error("MemberContact.InvalidEmail", "A valid email is required."));

        return new MemberContact(memberId, externalContactId, email, unsubscribed);
    }

    public void UpdateStatus(bool unsubscribed)
    {
        Unsubscribed = unsubscribed;
        Update();
    }

    public Result UpdateContactDetails(string externalContactId, string email)
    {
        if (string.IsNullOrWhiteSpace(externalContactId))
            return Result.Failure(new Error("MemberContact.InvalidExternalContactId", "External Contact ID cannot be empty."));

        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            return Result.Failure(new Error("MemberContact.InvalidEmail", "A valid email is required."));

        ExternalContactId = externalContactId;
        Email = email;
        Update();

        return Result.Success();
    }
}
