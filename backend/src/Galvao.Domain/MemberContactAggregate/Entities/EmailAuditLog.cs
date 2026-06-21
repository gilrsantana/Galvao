using Galvao.Domain.MemberContactAggregate.Enums;

namespace Galvao.Domain.MemberContactAggregate.Entities;

public class EmailAuditLog
{
    public Guid Id { get; private set; }
    public Guid? MemberId { get; private set; }
    public string RecipientEmail { get; private set; } = null!;
    public string Subject { get; private set; } = null!;
    public EMailProvider EMailProvider { get; private set; }
    public ETypeOfMessage ETypeOfMessage { get; private set; }
    public int StatusCode { get; private set; }
    public string? ExternalMessageId { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTime SentAtUtc { get; private set; }

    // EF Core Constructor
    private EmailAuditLog()
    { }

    private EmailAuditLog(
        Guid? memberId,
        string recipientEmail,
        string subject,
        EMailProvider eMailProvider,
        ETypeOfMessage typeOfMessage,
        int statusCode,
        string? externalMessageId,
        string? errorMessage)
    {
        Id = Guid.CreateVersion7();
        MemberId = memberId;
        RecipientEmail = recipientEmail;
        Subject = subject;
        EMailProvider = eMailProvider;
        ETypeOfMessage = typeOfMessage;
        StatusCode = statusCode;
        ExternalMessageId = externalMessageId;
        ErrorMessage = errorMessage;
        SentAtUtc = DateTime.UtcNow;
    }

    public static EmailAuditLog Create(
        Guid? memberId,
        string recipientEmail,
        string subject,
        EMailProvider eMailProvider,
        ETypeOfMessage typeOfMessage,
        int statusCode,
        string? externalMessageId,
        string? errorMessage)
    {
        return new EmailAuditLog(
            memberId,
            recipientEmail,
            subject,
            eMailProvider,
            typeOfMessage,
            statusCode,
            externalMessageId,
            errorMessage);
    }
}