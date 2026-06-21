using Moq;
using Galvao.Application.Common.Interfaces;
using Galvao.Application.UseCases.Showroom.Commands;
using Galvao.Application.UseCases.Showroom.CommandHandlers;
using Galvao.Domain.ShowroomAggregate.Entities;

namespace Galvao.UnitTests.Application;

public class CreateShowroomItemCommandHandlerTests
{
    private readonly Mock<IShowroomItemRepository> _showroomItemRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly CreateShowroomItemCommandHandler _handler;

    public CreateShowroomItemCommandHandlerTests()
    {
        _handler = new CreateShowroomItemCommandHandler(
            _showroomItemRepositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnSuccessWithId_WhenCommandIsValid()
    {
        // Arrange
        var command = new CreateShowroomItemCommand("Table", "A fine oak dining table", 599.99m, "Furniture");

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value);

        _showroomItemRepositoryMock.Verify(x => x.AddAsync(It.IsAny<ShowroomItem>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnFailure_WhenTitleIsEmpty()
    {
        // Arrange
        var command = new CreateShowroomItemCommand("", "A fine oak dining table", 599.99m, "Furniture");

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("ShowroomItem.TitleRequired", result.Error.Code);

        _showroomItemRepositoryMock.Verify(x => x.AddAsync(It.IsAny<ShowroomItem>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnFailure_WhenPriceIsNegative()
    {
        // Arrange
        var command = new CreateShowroomItemCommand("Table", "A fine oak dining table", -10.00m, "Furniture");

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("ShowroomItem.InvalidPrice", result.Error.Code);

        _showroomItemRepositoryMock.Verify(x => x.AddAsync(It.IsAny<ShowroomItem>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
