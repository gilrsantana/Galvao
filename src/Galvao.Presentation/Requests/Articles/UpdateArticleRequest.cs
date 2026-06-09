namespace Galvao.Presentation.Requests.Articles;

public record UpdateArticleRequest(
    string Title,
    string Content,
    string Author);
