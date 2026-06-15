using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Xunit;
using Galvao.Application.Common.Interfaces;
using Galvao.Presentation.Controllers;
using Galvao.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Galvao.UnitTests.Presentation;

public class AuthControllerTests
{
    private readonly Mock<IIdentityService> _identityServiceMock = new();
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _controller = new AuthController(_identityServiceMock.Object);
        
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/api/auth";
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    [Fact]
    public async Task ConfirmEmail_ShouldReturnOk_WhenSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var token = "valid-token";
        var request = new ConfirmEmailRequest(userId, token);

        _identityServiceMock
            .Setup(x => x.ConfirmEmailAsync(userId, token))
            .ReturnsAsync(Result.Success());

        // Act
        var response = await _controller.ConfirmEmail(request);

        // Assert
        Assert.IsType<OkResult>(response);
        _identityServiceMock.Verify(x => x.ConfirmEmailAsync(userId, token), Times.Once);
    }

    [Fact]
    public async Task ConfirmEmail_ShouldReturnBadRequest_WhenFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var token = "invalid-token";
        var request = new ConfirmEmailRequest(userId, token);
        var error = new Error("Auth.ConfirmEmailFailed", "Invalid token");

        _identityServiceMock
            .Setup(x => x.ConfirmEmailAsync(userId, token))
            .ReturnsAsync(Result.Failure(error));

        // Act
        var response = await _controller.ConfirmEmail(request);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(response);
        Assert.Equal(StatusCodes.Status400BadRequest, objectResult.StatusCode);
        
        var problemDetails = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal("Invalid token", problemDetails.Detail);
        
        _identityServiceMock.Verify(x => x.ConfirmEmailAsync(userId, token), Times.Once);
    }

    [Fact]
    public async Task ResendConfirmationEmail_ShouldReturnOk_WhenSuccess()
    {
        // Arrange
        var email = "test@galvao.com";
        var request = new ResendConfirmationEmailRequest(email);

        _identityServiceMock
            .Setup(x => x.ResendConfirmationEmailAsync(email))
            .ReturnsAsync(Result.Success());

        // Act
        var response = await _controller.ResendConfirmationEmail(request);

        // Assert
        Assert.IsType<OkResult>(response);
        _identityServiceMock.Verify(x => x.ResendConfirmationEmailAsync(email), Times.Once);
    }

    [Fact]
    public async Task ResendConfirmationEmail_ShouldReturnNotFound_WhenFailureIsAccountNotFound()
    {
        // Arrange
        var email = "test@galvao.com";
        var request = new ResendConfirmationEmailRequest(email);
        var error = new Error("Auth.AccountNotFound", "Account not found");

        _identityServiceMock
            .Setup(x => x.ResendConfirmationEmailAsync(email))
            .ReturnsAsync(Result.Failure(error));

        // Act
        var response = await _controller.ResendConfirmationEmail(request);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(response);
        Assert.Equal(StatusCodes.Status404NotFound, objectResult.StatusCode);

        var problemDetails = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal("Account not found", problemDetails.Detail);

        _identityServiceMock.Verify(x => x.ResendConfirmationEmailAsync(email), Times.Once);
    }
}
