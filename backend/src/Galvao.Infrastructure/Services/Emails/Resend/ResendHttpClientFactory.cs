namespace Galvao.Infrastructure.Services.Emails.Resend;

public enum ResendClientType
{
    Administrative,
    Email
}

public interface IResendHttpClientFactory
{
    HttpClient CreateClient(ResendClientType clientType);
}

public class ResendHttpClientFactory : IResendHttpClientFactory
{
    private readonly IHttpClientFactory _httpClientFactory;

    public ResendHttpClientFactory(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public HttpClient CreateClient(ResendClientType clientType)
    {
        return clientType switch
        {
            ResendClientType.Administrative => _httpClientFactory.CreateClient("ResendManagerClient"),
            ResendClientType.Email => _httpClientFactory.CreateClient("ResendSenderClient"),
            _ => throw new ArgumentOutOfRangeException(nameof(clientType), clientType, null)
        };
    }
}
