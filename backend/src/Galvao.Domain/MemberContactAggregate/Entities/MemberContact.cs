using Galvao.Domain.Base;
using Galvao.Domain.MemberContactAggregate.Enums;
using Galvao.Shared;

namespace Galvao.Domain.MemberContactAggregate.Entities;

public class MemberContact : BaseEntity
{
    private readonly List<EmailSegment> _emailSegments = [];
    public Guid MemberId { get; private set; }
    public string ExternalContactId { get; private set; }
    public EMailProvider EmailProvider { get; private set; }
    public string Email { get; private set; }
    public string? PhoneNumber { get; private set; }
    public IReadOnlyCollection<EmailSegment> EmailSegments => _emailSegments.AsReadOnly();

    // EF Core Constructor
    private MemberContact() : base()
    {
        ExternalContactId = string.Empty;
        Email = string.Empty;
    }

    private MemberContact(
        Guid memberId,
        string externalContactId,
        string email,
        string? phoneNumber,
        EMailProvider eMailProvider) : base()
    {
        MemberId = memberId;
        ExternalContactId = externalContactId;
        Email = email;
        PhoneNumber = phoneNumber;
        EmailProvider = eMailProvider;
    }

    public static Result<MemberContact> Create(
        Guid memberId,
        string externalContactId,
        string email,
        string? phoneNumber,
        EMailProvider eMailProvider = EMailProvider.Resend)
    {
        if (memberId == Guid.Empty)
            return Result.Failure<MemberContact>(new Error("MemberContact.InvalidMemberId", "Member ID cannot be empty."));

        if (string.IsNullOrWhiteSpace(externalContactId))
            return Result.Failure<MemberContact>(new Error("MemberContact.InvalidExternalContactId", "External Contact ID cannot be empty."));

        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            return Result.Failure<MemberContact>(new Error("MemberContact.InvalidEmail", "A valid email is required."));

        return new MemberContact(memberId, externalContactId, email, phoneNumber, eMailProvider);
    }

    public Result UpdateContactDetails(string externalContactId,
                                       string email,
                                       string? phoneNumber = null)
    {
        if (string.IsNullOrWhiteSpace(externalContactId))
            return Result.Failure(new Error("MemberContact.InvalidExternalContactId", "External Contact ID cannot be empty."));

        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            return Result.Failure(new Error("MemberContact.InvalidEmail", "A valid email is required."));

        ExternalContactId = externalContactId;
        Email = email;
        PhoneNumber = phoneNumber;

        Update();

        return Result.Success();
    }

    public Result<EmailSegment> AddEmailSegment(ESegmentType eSegmentType)
    {
        var existingEmailSegment = _emailSegments.FirstOrDefault(x => x.ESegmentType == eSegmentType);
        if (existingEmailSegment != null)
            return existingEmailSegment;

        var emailSegmentResult = EmailSegment.Create(Id, eSegmentType);
        if (emailSegmentResult.IsFailure)
            return Result.Failure<EmailSegment>(emailSegmentResult.Error);

        _emailSegments.Add(emailSegmentResult.Value);
        Update();
        return emailSegmentResult.Value;
    }

    public Result RemoveEmailSegment(ESegmentType eSegmentType)
    {
        var emailSegment = _emailSegments.FirstOrDefault(x => x.ESegmentType == eSegmentType);
        if (emailSegment == null)
            return Result.Failure(new Error("MemberContact.EmailSegmentNotFound", "Email segment not found."));

        _emailSegments.Remove(emailSegment);
        Update();
        return Result.Success();
    }

    public Result<EmailSegment> GetEmailSegment(ESegmentType eSegmentType)
    {
        var emailSegment = _emailSegments.FirstOrDefault(x => x.ESegmentType == eSegmentType);
        return emailSegment ??
            Result.Failure<EmailSegment>(new Error("MemberContact.EmailSegmentNotFound", "Email segment not found."));
    }

    public Result SetEmailSegmentUnsubscriptionDate(ESegmentType eSegmentType)
    {
        var emailSegment = GetEmailSegment(eSegmentType);
        if (emailSegment.IsFailure)
            return Result.Failure(new Error("MemberContact.EmailSegmentNotFound", $"Email segment not found. {eSegmentType}"));

        emailSegment.Value.SetUnSubscriptionDate(DateTime.Now);
        Update();
        return Result.Success();
    }

    public Result SetEmailSegmentSubscriptionDate(ESegmentType eSegmentType)
    {
        var emailSegment = GetEmailSegment(eSegmentType);
        if (emailSegment.IsFailure)
            return Result.Failure(new Error("MemberContact.EmailSegmentNotFound", $"Email segment not found. {eSegmentType}"));

        emailSegment.Value.SetSubscriptionDate(DateTime.Now);
        Update();
        return Result.Success();
    }
}
