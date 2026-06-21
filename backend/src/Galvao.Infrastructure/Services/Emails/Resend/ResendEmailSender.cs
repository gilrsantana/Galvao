using System.Net.Http.Json;
using Galvao.Application.Common.Interfaces;
using Galvao.Domain.MemberContactAggregate.Entities;
using Galvao.Domain.MemberContactAggregate.Enums;
using Galvao.Infrastructure.Configurations;
using Galvao.Infrastructure.Services.Emails.Resend.Models;
using Galvao.Shared;
using Microsoft.Extensions.Options;

namespace Galvao.Infrastructure.Services.Emails.Resend;

public class ResendEmailSender : IEmailSender
{
    private readonly HttpClient _httpClient;
    private readonly ResendSettings _settings;
    private readonly IEmailAuditLogRepository _emailAuditLogRepository;

    public ResendEmailSender(
        IResendHttpClientFactory httpClientFactory,
        IOptions<ResendSettings> settings,
        IEmailAuditLogRepository emailAuditLogRepository)
    {
        _httpClient = httpClientFactory.CreateClient(ResendClientType.Email);
        _settings = settings.Value;
        _emailAuditLogRepository = emailAuditLogRepository;
    }

    public async Task<Result> SendEmailAsync(
        List<string> to,
        string subject,
        string htmlContent,
        Guid? memberId = null,
        ETypeOfMessage? typeOfMessage = null,
        CancellationToken cancellationToken = default)
    {
        var recipientEmail = string.Join(",", to);
        var provider = EMailProvider.Resend;
        var messageType = typeOfMessage ?? ETypeOfMessage.EmailConfirmation;

        int statusCode = 200;
        string? externalMessageId = null;
        string? errorMessage = null;
        var name = "Onboarding";
        var nameFrom = "onboarding";
        var domain = "contact.gilmarsantana.com";
        var from = $"{name} <{nameFrom}@{domain}>";

        try
        {
            var request = new SendEmailRequest
            {
                From = from,
                To = to,
                Subject = subject,
                Html = htmlContent
            };

            var response = await _httpClient.PostAsJsonAsync("emails", request, cancellationToken);
            statusCode = (int)response.StatusCode;

            if (!response.IsSuccessStatusCode)
            {
                errorMessage = await response.Content.ReadAsStringAsync(cancellationToken);
                await LogEmailAuditAsync(memberId, recipientEmail, subject, provider, messageType, statusCode, externalMessageId, errorMessage, cancellationToken);

                return Result.Failure(new Error("EmailSender.SendFailed", $"Failed to send email via Resend: {response.ReasonPhrase}. Details: {errorMessage}"));
            }

            var result = await response.Content.ReadFromJsonAsync<SendEmailResponse>(cancellationToken: cancellationToken);
            if (result == null || string.IsNullOrEmpty(result.Id))
            {
                errorMessage = "Received an empty/invalid response body from Resend.";
                await LogEmailAuditAsync(memberId, recipientEmail, subject, provider, messageType, statusCode, externalMessageId, errorMessage, cancellationToken);

                return Result.Failure(new Error("EmailSender.InvalidResponse", errorMessage));
            }

            externalMessageId = result.Id;
            await LogEmailAuditAsync(memberId, recipientEmail, subject, provider, messageType, statusCode, externalMessageId, errorMessage, cancellationToken);

            return Result.Success();
        }
        catch (Exception ex)
        {
            statusCode = 500;
            errorMessage = ex.Message;
            await LogEmailAuditAsync(memberId, recipientEmail, subject, provider, messageType, statusCode, externalMessageId, errorMessage, cancellationToken);

            return Result.Failure(new Error("EmailSender.Exception", ex.Message));
        }
    }

    private async Task LogEmailAuditAsync(
        Guid? memberId,
        string recipientEmail,
        string subject,
        EMailProvider provider,
        ETypeOfMessage messageType,
        int statusCode,
        string? externalMessageId,
        string? errorMessage,
        CancellationToken cancellationToken)
    {
        var log = EmailAuditLog.Create(
            memberId,
            recipientEmail,
            subject,
            provider,
            messageType,
            statusCode,
            externalMessageId,
            errorMessage);

        await _emailAuditLogRepository.AddAsync(log, cancellationToken);
        await _emailAuditLogRepository.SaveChangesAsync(cancellationToken);
    }
}
