using System.Net.Http.Json;
using Galvao.Application.Common.Interfaces;
using Galvao.Infrastructure.Configurations;
using Galvao.Infrastructure.Services.Emails.Resend.Models;
using Galvao.Shared;
using Microsoft.Extensions.Options;

namespace Galvao.Infrastructure.Services.Emails.Resend;

public class ResendEmailSender : IEmailSender
{
    private readonly HttpClient _httpClient;
    private readonly ResendSettings _settings;

    public ResendEmailSender(IResendHttpClientFactory httpClientFactory, IOptions<ResendSettings> settings)
    {
        _httpClient = httpClientFactory.CreateClient(ResendClientType.Email);
        _settings = settings.Value;
    }

    public async Task<Result> SendEmailAsync(string to, string subject, string htmlContent, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new SendEmailRequest
            {
                From = _settings.FromEmail,
                To = new List<string> { to },
                Subject = subject,
                Html = htmlContent
            };

            var response = await _httpClient.PostAsJsonAsync("emails", request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorMsg = await response.Content.ReadAsStringAsync(cancellationToken);
                return Result.Failure(new Error("EmailSender.SendFailed", $"Failed to send email via Resend: {response.ReasonPhrase}. Details: {errorMsg}"));
            }

            var result = await response.Content.ReadFromJsonAsync<SendEmailResponse>(cancellationToken: cancellationToken);
            if (result == null || string.IsNullOrEmpty(result.Id))
            {
                return Result.Failure(new Error("EmailSender.InvalidResponse", "Received an invalid response from Resend."));
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure(new Error("EmailSender.Exception", ex.Message));
        }
    }
}
