using Galvao.Shared;

namespace Galvao.Domain.Entities;

public class ShowroomItem : BaseEntity
{
    private readonly List<ShowroomItemPhoto> _photos = new();

    public string Title { get; private set; }
    public string Description { get; private set; }
    public decimal Price { get; private set; }
    public string Category { get; private set; }
    public IReadOnlyCollection<ShowroomItemPhoto> Photos => _photos.AsReadOnly();

    // EF Core Constructor
    private ShowroomItem() : base()
    {
        Title = string.Empty;
        Description = string.Empty;
        Category = string.Empty;
    }

    private ShowroomItem(string title, string description, decimal price, string category) : base()
    {
        Title = title;
        Description = description;
        Price = price;
        Category = category;
    }

    public static Result<ShowroomItem> Create(string title, string description, decimal price, string category)
    {
        if (string.IsNullOrWhiteSpace(title))
            return Result.Failure<ShowroomItem>(new Error("ShowroomItem.TitleRequired", "Title is required."));

        if (string.IsNullOrWhiteSpace(description))
            return Result.Failure<ShowroomItem>(new Error("ShowroomItem.DescriptionRequired", "Description is required."));

        if (price < 0)
            return Result.Failure<ShowroomItem>(new Error("ShowroomItem.InvalidPrice", "Price cannot be negative."));

        if (string.IsNullOrWhiteSpace(category))
            return Result.Failure<ShowroomItem>(new Error("ShowroomItem.CategoryRequired", "Category is required."));

        return new ShowroomItem(title, description, price, category);
    }

    public Result UpdateDetails(string title, string description, decimal price, string category)
    {
        if (string.IsNullOrWhiteSpace(title))
            return Result.Failure(new Error("ShowroomItem.TitleRequired", "Title is required."));

        if (string.IsNullOrWhiteSpace(description))
            return Result.Failure(new Error("ShowroomItem.DescriptionRequired", "Description is required."));

        if (price < 0)
            return Result.Failure(new Error("ShowroomItem.InvalidPrice", "Price cannot be negative."));

        if (string.IsNullOrWhiteSpace(category))
            return Result.Failure(new Error("ShowroomItem.CategoryRequired", "Category is required."));

        Title = title;
        Description = description;
        Price = price;
        Category = category;
        Update();

        return Result.Success();
    }

    public Result<ShowroomItemPhoto> AddPhoto(string url, string caption, bool isPrimary)
    {
        var photoResult = ShowroomItemPhoto.Create(Id, url, caption, isPrimary);
        if (photoResult.IsFailure)
            return Result.Failure<ShowroomItemPhoto>(photoResult.Error);

        var photo = photoResult.Value;

        if (isPrimary || !_photos.Any())
        {
            photo.SetAsPrimary();
            foreach (var existingPhoto in _photos)
            {
                existingPhoto.ClearPrimary();
            }
        }

        _photos.Add(photo);
        Update();

        return photo;
    }

    public Result RemovePhoto(Guid photoId)
    {
        var photo = _photos.FirstOrDefault(p => p.Id == photoId);
        if (photo is null)
            return Result.Failure(new Error("ShowroomItem.PhotoNotFound", "Photo not found in this item."));

        _photos.Remove(photo);
        Update();

        // If we removed the primary photo, set the first remaining photo as primary
        if (photo.IsPrimary && _photos.Any())
        {
            _photos[0].SetAsPrimary();
        }

        return Result.Success();
    }

    public Result SetPrimaryPhoto(Guid photoId)
    {
        var photo = _photos.FirstOrDefault(p => p.Id == photoId);
        if (photo is null)
            return Result.Failure(new Error("ShowroomItem.PhotoNotFound", "Photo not found in this item."));

        foreach (var existingPhoto in _photos)
        {
            existingPhoto.ClearPrimary();
        }

        photo.SetAsPrimary();
        Update();

        return Result.Success();
    }

    public Result UpdatePhoto(Guid photoId, string url, string caption, bool isPrimary)
    {
        var photo = _photos.FirstOrDefault(p => p.Id == photoId);
        if (photo is null)
            return Result.Failure(new Error("ShowroomItem.PhotoNotFound", "Photo not found in this item."));

        if (string.IsNullOrWhiteSpace(url))
            return Result.Failure(new Error("ShowroomItemPhoto.UrlRequired", "Photo URL is required."));

        if (isPrimary)
        {
            foreach (var existingPhoto in _photos)
            {
                existingPhoto.ClearPrimary();
            }
        }

        var updateResult = photo.UpdateDetails(url, caption, isPrimary);
        if (updateResult.IsFailure)
            return updateResult;

        Update();

        return Result.Success();
    }
}
