using Galvao.Shared;

namespace Galvao.Domain.Entities;

public class ShowroomItemPhoto : BaseEntity
{
    public Guid ShowroomItemId { get; private set; }
    public string Url { get; private set; }
    public string Caption { get; private set; }
    public bool IsPrimary { get; private set; }

    // EF Core Constructor
    private ShowroomItemPhoto() : base()
    {
        Url = string.Empty;
        Caption = string.Empty;
    }

    private ShowroomItemPhoto(Guid showroomItemId, string url, string caption, bool isPrimary) : base()
    {
        ShowroomItemId = showroomItemId;
        Url = url;
        Caption = caption;
        IsPrimary = isPrimary;
    }

    public static Result<ShowroomItemPhoto> Create(Guid showroomItemId, string url, string caption, bool isPrimary)
    {
        if (showroomItemId == Guid.Empty)
            return Result.Failure<ShowroomItemPhoto>(new Error("ShowroomItemPhoto.InvalidShowroomItemId", "Showroom Item ID cannot be empty."));

        if (string.IsNullOrWhiteSpace(url))
            return Result.Failure<ShowroomItemPhoto>(new Error("ShowroomItemPhoto.UrlRequired", "Photo URL is required."));

        return new ShowroomItemPhoto(showroomItemId, url, caption ?? string.Empty, isPrimary);
    }

    public void SetAsPrimary()
    {
        IsPrimary = true;
        Update();
    }

    public void ClearPrimary()
    {
        IsPrimary = false;
        Update();
    }
}
