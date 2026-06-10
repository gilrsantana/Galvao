using Moq;
using Galvao.Application.Common.Interfaces;
using Galvao.Application.UseCases.Members.Queries;
using Galvao.Application.UseCases.Members.QueryHandlers;
using Galvao.Domain.Entities;
using Galvao.Shared;
using Xunit;

namespace Galvao.UnitTests.Application;

public class GetMemberByIdQueryHandlerTests
{
    private readonly Mock<IMemberRepository> _memberRepositoryMock = new();
    private readonly GetMemberByIdQueryHandler _handler;

    public GetMemberByIdQueryHandlerTests()
    {
        _handler = new GetMemberByIdQueryHandler(_memberRepositoryMock.Object);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnSuccess_WhenMemberExists()
    {
        // Arrange
        var member = Member.Create("test@galvao.com", "Display Name", "First", "Last", acceptNews: true, acceptPromo: false).Value;
        var query = new GetMemberByIdQuery(member.Id);

        _memberRepositoryMock
            .Setup(x => x.GetByIdAsync(member.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(member.Id, result.Value.Id);
        Assert.Equal(member.Email, result.Value.Email);
        Assert.Equal(member.DisplayName, result.Value.DisplayName);
        Assert.Equal(member.FirstName, result.Value.FirstName);
        Assert.Equal(member.LastName, result.Value.LastName);
        Assert.Equal(member.Active, result.Value.Active);
        Assert.Equal(member.CreatedAt, result.Value.CreatedAt);
        Assert.Equal(member.UpdatedAt, result.Value.UpdatedAt);
        Assert.Equal(member.AcceptNews, result.Value.AcceptNews);
        Assert.Equal(member.AcceptPromo, result.Value.AcceptPromo);

        _memberRepositoryMock.Verify(x => x.GetByIdAsync(member.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnFailure_WhenMemberDoesNotExist()
    {
        // Arrange
        var memberId = Guid.NewGuid();
        var query = new GetMemberByIdQuery(memberId);

        _memberRepositoryMock
            .Setup(x => x.GetByIdAsync(memberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Member?)null);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Member.NotFound", result.Error.Code);

        _memberRepositoryMock.Verify(x => x.GetByIdAsync(memberId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
