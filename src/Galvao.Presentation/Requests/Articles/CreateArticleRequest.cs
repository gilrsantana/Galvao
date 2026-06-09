namespace Galvao.Presentation.Requests.Articles;

public record CreateArticleRequest(
    string Title,
    string Content,
    string Author);
