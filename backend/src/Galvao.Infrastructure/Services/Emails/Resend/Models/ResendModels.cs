using System.Text.Json.Serialization;

namespace Galvao.Infrastructure.Services.Emails.Resend.Models;

public class Segment
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("created_at")] public string CreatedAt { get; set; } = string.Empty;
}

public class Contact
{
    [JsonPropertyName("object")]
    public string Object { get; set; } = "contact";

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("first_name")]
    public string? FirstName { get; set; }

    [JsonPropertyName("last_name")]
    public string? LastName { get; set; }

    [JsonPropertyName("unsubscribed")]
    public bool Unsubscribed { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }
}

public class CreateSegmentRequest
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

public class SegmentRef
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }
}

public class CreateContactRequest
{
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("first_name")]
    public string? FirstName { get; set; }

    [JsonPropertyName("last_name")]
    public string? LastName { get; set; }

    [JsonPropertyName("unsubscribed")]
    public bool Unsubscribed { get; set; }

    [JsonPropertyName("segments")]
    public List<SegmentRef>? Segments { get; set; }
}

public class UpdateContactRequest
{
    [JsonPropertyName("first_name")]
    public string? FirstName { get; set; }

    [JsonPropertyName("last_name")]
    public string? LastName { get; set; }

    [JsonPropertyName("unsubscribed")]
    public bool? Unsubscribed { get; set; }
}

public class CreateSegmentResponse
{
    [JsonPropertyName("object")]
    public string Object { get; set; } = string.Empty;

    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

public class ContactMutationResponse
{
    [JsonPropertyName("object")]
    public string Object { get; set; } = string.Empty;

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
}

public class ListSegmentsResponse
{
    [JsonPropertyName("object")]
    public string Object { get; set; } = string.Empty;

    [JsonPropertyName("has_more")]
    public bool HasMore { get; set; }

    [JsonPropertyName("data")]
    public List<Segment> Data { get; set; } = new();
}

public class ListContactsResponse
{
    [JsonPropertyName("object")]
    public string Object { get; set; } = string.Empty;

    [JsonPropertyName("has_more")]
    public bool HasMore { get; set; }

    [JsonPropertyName("data")]
    public List<Contact> Data { get; set; } = new();
}

public class SendEmailRequest
{
    [JsonPropertyName("from")]
    public string From { get; set; } = string.Empty;

    [JsonPropertyName("to")]
    public List<string> To { get; set; } = new();

    [JsonPropertyName("subject")]
    public string Subject { get; set; } = string.Empty;

    [JsonPropertyName("html")]
    public string Html { get; set; } = string.Empty;
}

public class SendEmailResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
}
