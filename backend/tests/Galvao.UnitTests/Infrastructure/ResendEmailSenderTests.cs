using System.Net;
using System.Net.Http.Json;
using Galvao.Application.Common.Interfaces;
using Galvao.Domain.MemberContactAggregate.Entities;
using Galvao.Domain.MemberContactAggregate.Enums;
using Galvao.Infrastructure.Configurations;
using Galvao.Infrastructure.Services.Emails.Resend;
using Galvao.Infrastructure.Services.Emails.Resend.Models;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;

namespace Galvao.UnitTests.Infrastructure;

public class ResendEmailSenderTests
{
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock = new();
    private readonly Mock<IResendHttpClientFactory> _httpClientFactoryMock = new();
    private readonly Mock<IEmailAuditLogRepository> _emailAuditLogRepositoryMock = new();
    private readonly ResendSettings _settings;
    private readonly IOptions<ResendSettings> _options;

    public ResendEmailSenderTests()
    {
        _settings = new ResendSettings
        {
            FromEmail = "sender@galvao.com",
            SenderApiKey = "re_test_sender_key"
        };
        _options = Options.Create(_settings);
    }

    private ResendEmailSender CreateSender(HttpClient httpClient)
    {
        _httpClientFactoryMock
            .Setup(f => f.CreateClient(It.IsAny<ResendClientType>()))
            .Returns(httpClient);

        return new ResendEmailSender(
            _httpClientFactoryMock.Object,
            _options,
            _emailAuditLogRepositoryMock.Object);
    }

    [Fact]
    public async Task SendEmailAsync_ShouldLogSuccess_WhenApiCallSucceeds()
    {
        // Arrange
        var to = new List<string> { "recipient@galvao.com" };
        var subject = "Test Subject";
        var htmlContent = "<p>Test Content</p>";
        var memberId = Guid.NewGuid();
        var messageType = ETypeOfMessage.EmailConfirmation;

        var emailResponse = new SendEmailResponse
        {
            Id = "email_12345"
        };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = JsonContent.Create(emailResponse)
            });

        var httpClient = new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri("https://api.resend.com/")
        };

        var sender = CreateSender(httpClient);

        // Act
        var result = await sender.SendEmailAsync(to, subject, htmlContent, memberId, messageType, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);

        _emailAuditLogRepositoryMock.Verify(x => x.AddAsync(
            It.Is<EmailAuditLog>(log =>
                log.MemberId == memberId &&
                log.RecipientEmail == "recipient@galvao.com" &&
                log.Subject == subject &&
                log.EMailProvider == EMailProvider.Resend &&
                log.ETypeOfMessage == messageType &&
                log.StatusCode == 200 &&
                log.ExternalMessageId == "email_12345" &&
                log.ErrorMessage == null),
            It.IsAny<CancellationToken>()),
            Times.Once);

        _emailAuditLogRepositoryMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendEmailAsync_ShouldLogFailure_WhenApiCallReturnsError()
    {
        // Arrange
        var to = new List<string> { "recipient@galvao.com" };
        var subject = "Test Subject";
        var htmlContent = "<p>Test Content</p>";
        var memberId = Guid.NewGuid();
        var messageType = ETypeOfMessage.EmailConfirmation;

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent("Invalid Request Parameters")
            });

        var httpClient = new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri("https://api.resend.com/")
        };

        var sender = CreateSender(httpClient);

        // Act
        var result = await sender.SendEmailAsync(to, subject, htmlContent, memberId, messageType, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("EmailSender.SendFailed", result.Error.Code);

        _emailAuditLogRepositoryMock.Verify(x => x.AddAsync(
            It.Is<EmailAuditLog>(log =>
                log.MemberId == memberId &&
                log.RecipientEmail == "recipient@galvao.com" &&
                log.Subject == subject &&
                log.EMailProvider == EMailProvider.Resend &&
                log.ETypeOfMessage == messageType &&
                log.StatusCode == 400 &&
                log.ExternalMessageId == null &&
                log.ErrorMessage == "Invalid Request Parameters"),
            It.IsAny<CancellationToken>()),
            Times.Once);

        _emailAuditLogRepositoryMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendEmailAsync_ShouldLogFailure_WhenApiResponseIsEmpty()
    {
        // Arrange
        var to = new List<string> { "recipient@galvao.com" };
        var subject = "Test Subject";
        var htmlContent = "<p>Test Content</p>";
        var memberId = Guid.NewGuid();
        var messageType = ETypeOfMessage.EmailConfirmation;

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = JsonContent.Create(new { }) // Valid JSON but missing Id
            });

        var httpClient = new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri("https://api.resend.com/")
        };

        var sender = CreateSender(httpClient);

        // Act
        var result = await sender.SendEmailAsync(to, subject, htmlContent, memberId, messageType, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("EmailSender.InvalidResponse", result.Error.Code);

        _emailAuditLogRepositoryMock.Verify(x => x.AddAsync(
            It.Is<EmailAuditLog>(log =>
                log.MemberId == memberId &&
                log.RecipientEmail == "recipient@galvao.com" &&
                log.Subject == subject &&
                log.EMailProvider == EMailProvider.Resend &&
                log.ETypeOfMessage == messageType &&
                log.StatusCode == 200 &&
                log.ExternalMessageId == null &&
                log.ErrorMessage == "Received an empty/invalid response body from Resend."),
            It.IsAny<CancellationToken>()),
            Times.Once);

        _emailAuditLogRepositoryMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendEmailAsync_ShouldLogException_WhenHttpClientThrowsException()
    {
        // Arrange
        var to = new List<string> { "recipient@galvao.com" };
        var subject = "Test Subject";
        var htmlContent = "<p>Test Content</p>";
        var memberId = Guid.NewGuid();
        var messageType = ETypeOfMessage.EmailConfirmation;

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ThrowsAsync(new HttpRequestException("Connection timeout"));

        var httpClient = new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri("https://api.resend.com/")
        };

        var sender = CreateSender(httpClient);

        // Act
        var result = await sender.SendEmailAsync(to, subject, htmlContent, memberId, messageType, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("EmailSender.Exception", result.Error.Code);

        _emailAuditLogRepositoryMock.Verify(x => x.AddAsync(
            It.Is<EmailAuditLog>(log =>
                log.MemberId == memberId &&
                log.RecipientEmail == "recipient@galvao.com" &&
                log.Subject == subject &&
                log.EMailProvider == EMailProvider.Resend &&
                log.ETypeOfMessage == messageType &&
                log.StatusCode == 500 &&
                log.ExternalMessageId == null &&
                log.ErrorMessage == "Connection timeout"),
            It.IsAny<CancellationToken>()),
            Times.Once);

        _emailAuditLogRepositoryMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
