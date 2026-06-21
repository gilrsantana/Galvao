using System.Net;
using System.Net.Http.Json;
using Galvao.Infrastructure.Configurations;
using Galvao.Infrastructure.Services.Emails.Resend;
using Galvao.Infrastructure.Services.Emails.Resend.Models;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;

namespace Galvao.UnitTests.Infrastructure;

public class ResendEmailContactServiceTests
{
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock = new();
    private readonly ResendSettings _settings;
    private readonly IOptions<ResendSettings> _options;
    private readonly Mock<IResendHttpClientFactory> _httpClientFactoryMock = new();

    public ResendEmailContactServiceTests()
    {
        _settings = new ResendSettings
        {
            ManagerApiKey = "re_test_key",
            SenderApiKey = "re_test_sender_key",
            ClientName = "galvao"
        };
        _options = Options.Create(_settings);
    }

    [Fact]
    public async Task CreateContactAsync_ShouldReturnExternalContactId_WhenApiCallSucceeds()
    {
        // Arrange
        var segmentsResponse = new ListSegmentsResponse
        {
            Data = new List<Segment>
            {
                new Segment { Id = Guid.NewGuid(), Name = "galvao_lead_news" },
                new Segment { Id = Guid.NewGuid(), Name = "galvao_lead_promo" }
            }
        };

        var contactResponse = new ContactMutationResponse
        {
            Id = "contact_12345",
            Object = "contact"
        };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync((HttpRequestMessage request, CancellationToken cancellationToken) =>
            {
                var path = request.RequestUri?.AbsolutePath ?? "";
                if (request.Method == HttpMethod.Get && path.EndsWith("segments", StringComparison.OrdinalIgnoreCase))
                {
                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = JsonContent.Create(segmentsResponse)
                    };
                }
                if (request.Method == HttpMethod.Post && path.EndsWith("contacts", StringComparison.OrdinalIgnoreCase))
                {
                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = JsonContent.Create(contactResponse)
                    };
                }
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            });

        var httpClient = new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri("https://api.resend.com/")
        };

        _httpClientFactoryMock
            .Setup(f => f.CreateClient(It.IsAny<ResendClientType>()))
            .Returns(httpClient);

        var service = new ResendEmailContactService(_httpClientFactoryMock.Object, _options);

        // Act
        var result = await service.CreateContactAsync("test@gmail.com", "John", "Doe", acceptNews: true, acceptPromo: true);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("contact_12345", result.Value);
    }
}
