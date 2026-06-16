using Moq;
using Galvao.Application.Common.Interfaces;
using Galvao.Application.UseCases.Articles.Commands;
using Galvao.Application.UseCases.Articles.CommandHandlers;
using Galvao.Domain.Entities;

namespace Galvao.UnitTests.Application;

public class PublishArticleCommandHandlerTests
{
    private readonly Mock<IArticleRepository> _articleRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly PublishArticleCommandHandler _handler;

    public PublishArticleCommandHandlerTests()
    {
        _handler = new PublishArticleCommandHandler(
            _articleRepositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task HandleAsync_ShouldPublishArticle_WhenNotPublished()
    {
        // Arrange
        var articleId = Guid.NewGuid();
        var article = Article.Create("Title", "Content", "Author").Value;
        var command = new PublishArticleCommand(articleId, true);

        _articleRepositoryMock
            .Setup(x => x.GetByIdAsync(articleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(article);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(article.IsPublished);
        Assert.NotNull(article.PublishedAt);

        _articleRepositoryMock.Verify(x => x.Update(article), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnFailure_WhenAlreadyPublished()
    {
        // Arrange
        var articleId = Guid.NewGuid();
        var article = Article.Create("Title", "Content", "Author").Value;
        article.Publish(); // Already published
        var command = new PublishArticleCommand(articleId, true);

        _articleRepositoryMock
            .Setup(x => x.GetByIdAsync(articleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(article);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Article.AlreadyPublished", result.Error.Code);

        _articleRepositoryMock.Verify(x => x.Update(It.IsAny<Article>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldUnpublishArticle_WhenPublished()
    {
        // Arrange
        var articleId = Guid.NewGuid();
        var article = Article.Create("Title", "Content", "Author").Value;
        article.Publish(); // Publish first
        var command = new PublishArticleCommand(articleId, false);

        _articleRepositoryMock
            .Setup(x => x.GetByIdAsync(articleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(article);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(article.IsPublished);
        Assert.Null(article.PublishedAt);

        _articleRepositoryMock.Verify(x => x.Update(article), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
