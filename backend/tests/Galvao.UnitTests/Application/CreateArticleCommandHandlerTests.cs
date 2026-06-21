using Moq;
using Galvao.Application.Common.Interfaces;
using Galvao.Application.UseCases.Articles.Commands;
using Galvao.Application.UseCases.Articles.CommandHandlers;
using Galvao.Domain.ArticleAggregate.Entities;

namespace Galvao.UnitTests.Application;

public class CreateArticleCommandHandlerTests
{
    private readonly Mock<IArticleRepository> _articleRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly CreateArticleCommandHandler _handler;

    public CreateArticleCommandHandlerTests()
    {
        _handler = new CreateArticleCommandHandler(
            _articleRepositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnSuccessWithId_WhenCommandIsValid()
    {
        // Arrange
        var command = new CreateArticleCommand("Living Room Guide", "Tips to decorate your living room.", "Jane Doe");

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value);

        _articleRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Article>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnFailure_WhenTitleIsEmpty()
    {
        // Arrange
        var command = new CreateArticleCommand("", "Tips to decorate your living room.", "Jane Doe");

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Article.TitleRequired", result.Error.Code);

        _articleRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Article>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
