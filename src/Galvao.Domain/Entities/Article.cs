using Galvao.Shared;

namespace Galvao.Domain.Entities;

public class Article : BaseEntity
{
    public string Title { get; private set; }
    public string Content { get; private set; }
    public string Author { get; private set; }
    public DateTime? PublishedAt { get; private set; }
    public bool IsPublished { get; private set; }

    // EF Core Constructor
    private Article() : base()
    {
        Title = string.Empty;
        Content = string.Empty;
        Author = string.Empty;
    }

    private Article(string title, string content, string author) : base()
    {
        Title = title;
        Content = content;
        Author = author;
        IsPublished = false;
    }

    public static Result<Article> Create(string title, string content, string author)
    {
        if (string.IsNullOrWhiteSpace(title))
            return Result.Failure<Article>(new Error("Article.TitleRequired", "Title is required."));

        if (string.IsNullOrWhiteSpace(content))
            return Result.Failure<Article>(new Error("Article.ContentRequired", "Content is required."));

        if (string.IsNullOrWhiteSpace(author))
            return Result.Failure<Article>(new Error("Article.AuthorRequired", "Author is required."));

        return new Article(title, content, author);
    }

    public Result UpdateContent(string title, string content, string author)
    {
        if (string.IsNullOrWhiteSpace(title))
            return Result.Failure(new Error("Article.TitleRequired", "Title is required."));

        if (string.IsNullOrWhiteSpace(content))
            return Result.Failure(new Error("Article.ContentRequired", "Content is required."));

        if (string.IsNullOrWhiteSpace(author))
            return Result.Failure(new Error("Article.AuthorRequired", "Author is required."));

        Title = title;
        Content = content;
        Author = author;
        Update();

        return Result.Success();
    }

    public Result Publish()
    {
        if (IsPublished)
            return Result.Failure(new Error("Article.AlreadyPublished", "Article is already published."));

        IsPublished = true;
        PublishedAt = DateTime.Now;
        Update();

        return Result.Success();
    }

    public Result Unpublish()
    {
        if (!IsPublished)
            return Result.Failure(new Error("Article.NotPublished", "Article is not published."));

        IsPublished = false;
        PublishedAt = null;
        Update();

        return Result.Success();
    }
}
