using Galvao.Domain.Base;
using Galvao.Domain.MemberContactAggregate.Enums;
using Galvao.Shared;

namespace Galvao.Domain.MemberContactAggregate.Entities;

public class EmailSegment : BaseEntity
{
    public Guid MemberContactId { get; private set; }
    public MemberContact? MemberContact { get; private set; }
    public ESegmentType ESegmentType { get; private set; }
    public DateTime SubscriptionDate { get; private set; }
    public DateTime? UnSubscriptionDate { get; private set; }

    private EmailSegment() : base()
    { }

    private EmailSegment(Guid memberContactId, ESegmentType eSegmentType)
    {
        MemberContactId = memberContactId;
        ESegmentType = eSegmentType;
        SubscriptionDate = DateTime.Now;
    }

    internal static Result<EmailSegment> Create(Guid memberContactId, ESegmentType eSegmentType)
    {
        return new EmailSegment(memberContactId, eSegmentType);
    }

    internal void SetSubscriptionDate(DateTime date)
    {
        SubscriptionDate = date;
        UnSubscriptionDate = null;
        base.Update();
    }
    internal void SetUnSubscriptionDate(DateTime date)
    {
        UnSubscriptionDate = date;
        base.Update();
    }
}