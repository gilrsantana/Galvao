using System.Net.Http.Json;
using Galvao.Application.Common.Interfaces;
using Galvao.Infrastructure.Configurations;
using Galvao.Infrastructure.Services.Emails.Resend.Models;
using Galvao.Shared;
using Microsoft.Extensions.Options;

namespace Galvao.Infrastructure.Services.Emails.Resend;

public class ResendEmailContactService : IEmailContactService
{
    private readonly HttpClient _httpClient;
    private readonly ResendSettings _settings;

    public ResendEmailContactService(IResendHttpClientFactory httpClientFactory, IOptions<ResendSettings> settings)
    {
        _httpClient = httpClientFactory.CreateClient(ResendClientType.Administrative);
        _settings = settings.Value;
    }

    public async Task<Result<string>> CreateContactAsync(
        string email,
        string firstName,
        string lastName,
        bool acceptNews,
        bool acceptPromo,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var segments = new List<SegmentRef>();

            if (acceptNews)
            {
                var segmentIdResult = await GetOrCreateSegmentAsync($"{_settings.ClientName}_lead_news", cancellationToken);
                if (segmentIdResult.IsFailure) return Result.Failure<string>(segmentIdResult.Error);
                segments.Add(new SegmentRef { Id = segmentIdResult.Value });
            }

            if (acceptPromo)
            {
                var segmentIdResult = await GetOrCreateSegmentAsync($"{_settings.ClientName}_lead_promo", cancellationToken);
                if (segmentIdResult.IsFailure) return Result.Failure<string>(segmentIdResult.Error);
                segments.Add(new SegmentRef { Id = segmentIdResult.Value });
            }

            var request = new CreateContactRequest
            {
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                Unsubscribed = false,
                Segments = segments.Any() ? segments : null
            };

            var response = await _httpClient.PostAsJsonAsync("contacts", request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorMsg = await response.Content.ReadAsStringAsync(cancellationToken);
                return Result.Failure<string>(new Error("EmailContact.CreateFailed", $"Failed to create Resend contact: {response.ReasonPhrase}. Details: {errorMsg}"));
            }

            var result = await response.Content.ReadFromJsonAsync<ContactMutationResponse>(cancellationToken: cancellationToken);
            if (result == null || string.IsNullOrEmpty(result.Id))
            {
                return Result.Failure<string>(new Error("EmailContact.InvalidResponse", "Received an invalid response from Resend."));
            }

            return result.Id;
        }
        catch (Exception ex)
        {
            return Result.Failure<string>(new Error("EmailContact.Exception", ex.Message));
        }
    }

    public async Task<Result> UpdateContactAsync(
        string externalContactId,
        string firstName,
        string lastName,
        bool unsubscribed,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new UpdateContactRequest
            {
                FirstName = firstName,
                LastName = lastName,
                Unsubscribed = unsubscribed
            };

            var response = await _httpClient.PatchAsJsonAsync($"contacts/{externalContactId}", request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorMsg = await response.Content.ReadAsStringAsync(cancellationToken);
                return Result.Failure(new Error("EmailContact.UpdateFailed", $"Failed to update Resend contact '{externalContactId}': {response.ReasonPhrase}. Details: {errorMsg}"));
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure(new Error("EmailContact.Exception", ex.Message));
        }
    }

    public async Task<Result> DeleteContactAsync(
        string externalContactId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"contacts/{externalContactId}", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorMsg = await response.Content.ReadAsStringAsync(cancellationToken);
                return Result.Failure(new Error("EmailContact.DeleteFailed", $"Failed to delete Resend contact '{externalContactId}': {response.ReasonPhrase}. Details: {errorMsg}"));
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure(new Error("EmailContact.Exception", ex.Message));
        }
    }

    private async Task<Result<Guid>> GetOrCreateSegmentAsync(string segmentName, CancellationToken cancellationToken)
    {
        // 1. Get segments
        var response = await _httpClient.GetAsync("segments", cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            var listResult = await response.Content.ReadFromJsonAsync<ListSegmentsResponse>(cancellationToken: cancellationToken);
            var existingSegment = listResult?.Data?.FirstOrDefault(s => s.Name.Equals(segmentName, StringComparison.OrdinalIgnoreCase));
            if (existingSegment != null)
            {
                return existingSegment.Id;
            }
        }

        // 2. Not found, create it
        var createRequest = new CreateSegmentRequest { Name = segmentName };
        var createResponse = await _httpClient.PostAsJsonAsync("segments", createRequest, cancellationToken);
        if (!createResponse.IsSuccessStatusCode)
        {
            var errorMsg = await createResponse.Content.ReadAsStringAsync(cancellationToken);
            return Result.Failure<Guid>(new Error("EmailContact.SegmentCreationFailed", $"Failed to create segment '{segmentName}': {createResponse.ReasonPhrase}. Details: {errorMsg}"));
        }

        var createResult = await createResponse.Content.ReadFromJsonAsync<CreateSegmentResponse>(cancellationToken: cancellationToken);
        if (createResult == null || createResult.Id == Guid.Empty)
        {
            return Result.Failure<Guid>(new Error("EmailContact.InvalidSegmentResponse", $"Received an invalid segment response for '{segmentName}'."));
        }

        return createResult.Id;
    }
}
