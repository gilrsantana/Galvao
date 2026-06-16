using Moq;
using Galvao.Application.Common.Interfaces;
using Galvao.Application.UseCases.Showroom.Commands;
using Galvao.Application.UseCases.Showroom.CommandHandlers;
using Galvao.Domain.Entities;

namespace Galvao.UnitTests.Application;

public class UpdateShowroomItemPhotoCommandHandlerTests
{
    private readonly Mock<IShowroomItemRepository> _showroomItemRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly UpdateShowroomItemPhotoCommandHandler _handler;

    public UpdateShowroomItemPhotoCommandHandlerTests()
    {
        _handler = new UpdateShowroomItemPhotoCommandHandler(
            _showroomItemRepositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnSuccess_WhenCommandIsValid()
    {
        // Arrange
        var itemId = Guid.NewGuid();
        var item = ShowroomItem.Create("Table", "A dining table", 100m, "Furniture").Value;
        var photoResult = item.AddPhoto("http://old-url.com", "Old caption", true);
        var photoId = photoResult.Value.Id;

        _showroomItemRepositoryMock
            .Setup(x => x.GetByIdAsync(itemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        var command = new UpdateShowroomItemPhotoCommand(itemId, photoId, "http://new-url.com", "New caption", true);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("http://new-url.com", photoResult.Value.Url);
        Assert.Equal("New caption", photoResult.Value.Caption);
        Assert.True(photoResult.Value.IsPrimary);

        _showroomItemRepositoryMock.Verify(x => x.Update(item), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnFailure_WhenShowroomItemNotFound()
    {
        // Arrange
        var itemId = Guid.NewGuid();
        var photoId = Guid.NewGuid();

        _showroomItemRepositoryMock
            .Setup(x => x.GetByIdAsync(itemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShowroomItem?)null);

        var command = new UpdateShowroomItemPhotoCommand(itemId, photoId, "http://new-url.com", "New caption", true);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("ShowroomItem.NotFound", result.Error.Code);

        _showroomItemRepositoryMock.Verify(x => x.Update(It.IsAny<ShowroomItem>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnFailure_WhenPhotoNotFound()
    {
        // Arrange
        var itemId = Guid.NewGuid();
        var item = ShowroomItem.Create("Table", "A dining table", 100m, "Furniture").Value;
        var photoId = Guid.NewGuid(); // Random non-existing photo ID

        _showroomItemRepositoryMock
            .Setup(x => x.GetByIdAsync(itemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        var command = new UpdateShowroomItemPhotoCommand(itemId, photoId, "http://new-url.com", "New caption", true);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("ShowroomItem.PhotoNotFound", result.Error.Code);

        _showroomItemRepositoryMock.Verify(x => x.Update(It.IsAny<ShowroomItem>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnFailure_WhenUrlIsEmpty()
    {
        // Arrange
        var itemId = Guid.NewGuid();
        var item = ShowroomItem.Create("Table", "A dining table", 100m, "Furniture").Value;
        var photoResult = item.AddPhoto("http://old-url.com", "Old caption", true);
        var photoId = photoResult.Value.Id;

        _showroomItemRepositoryMock
            .Setup(x => x.GetByIdAsync(itemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        var command = new UpdateShowroomItemPhotoCommand(itemId, photoId, "", "New caption", true);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("ShowroomItemPhoto.UrlRequired", result.Error.Code);

        _showroomItemRepositoryMock.Verify(x => x.Update(It.IsAny<ShowroomItem>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
