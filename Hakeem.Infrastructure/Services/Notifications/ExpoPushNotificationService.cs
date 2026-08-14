using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Hakeem.Application.Interfaces.Notifications;

namespace Hakeem.Infrastructure.Services;

public sealed class ExpoPushNotificationService(HttpClient httpClient)
    : IPushNotificationService
{
    private const int MaximumBatchSize = 100;

    public async Task<PushNotificationSendResult> SendAsync(
        PushNotificationMessage message,
        CancellationToken cancellationToken)
    {
        if (message.DeviceTokens.Count == 0)
        {
            return PushNotificationSendResult.Success;
        }

        var inactiveTokens = new HashSet<string>(StringComparer.Ordinal);
        var tokens = message.DeviceTokens
            .Where(token => !string.IsNullOrWhiteSpace(token))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        foreach (var batch in tokens.Chunk(MaximumBatchSize))
        {
            var requests = batch
                .Select(token => new ExpoPushMessage(
                    token,
                    message.Title,
                    message.Body,
                    message.Data))
                .ToArray();

            using var response = await httpClient.PostAsJsonAsync(
                "send",
                requests,
                cancellationToken);

            if (response.StatusCode == HttpStatusCode.TooManyRequests ||
                (int)response.StatusCode >= 500)
            {
                throw new HttpRequestException(
                    $"Expo push service returned transient HTTP status {(int)response.StatusCode}.",
                    inner: null,
                    response.StatusCode);
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"Expo push service rejected the request with HTTP status {(int)response.StatusCode}.");
            }

            await ProcessResponseAsync(
                response,
                batch,
                inactiveTokens,
                cancellationToken);
        }

        return inactiveTokens.Count == 0
            ? PushNotificationSendResult.Success
            : new PushNotificationSendResult(inactiveTokens.ToArray());
    }

    private static async Task ProcessResponseAsync(
        HttpResponseMessage response,
        IReadOnlyList<string> tokens,
        ISet<string> inactiveTokens,
        CancellationToken cancellationToken)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(
            cancellationToken);

        var expoResponse = await JsonSerializer.DeserializeAsync<ExpoPushResponse>(
            stream,
            cancellationToken: cancellationToken);

        if (expoResponse is null)
        {
            throw new InvalidOperationException(
                "Expo push service returned an invalid response.");
        }

        if (expoResponse.Errors is { Count: > 0 })
        {
            if (expoResponse.Errors.Any(error =>
                    string.Equals(
                        error.Code,
                        "TOO_MANY_REQUESTS",
                        StringComparison.OrdinalIgnoreCase)))
            {
                throw new HttpRequestException(
                    "Expo push service reported a transient rate-limit error.");
            }

            throw new InvalidOperationException(
                "Expo push service rejected the notification batch.");
        }

        var tickets = ReadTickets(expoResponse.Data);
        if (tickets.Count != tokens.Count)
        {
            throw new InvalidOperationException(
                "Expo push service returned an unexpected number of push tickets.");
        }

        for (var index = 0; index < tickets.Count; index++)
        {
            var ticket = tickets[index];
            if (string.Equals(ticket.Status, "ok", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var error = ticket.Details?.Error;
            if (string.Equals(
                    error,
                    "DeviceNotRegistered",
                    StringComparison.OrdinalIgnoreCase))
            {
                inactiveTokens.Add(tokens[index]);
                continue;
            }

            if (string.Equals(
                    error,
                    "MessageRateExceeded",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new HttpRequestException(
                    "Expo push service reported a transient device rate-limit error.");
            }

            throw new InvalidOperationException(
                "Expo push service returned a permanent push ticket error.");
        }
    }

    private static IReadOnlyList<ExpoPushTicket> ReadTickets(JsonElement data)
    {
        if (data.ValueKind == JsonValueKind.Array)
        {
            return data.Deserialize<List<ExpoPushTicket>>() ?? [];
        }

        if (data.ValueKind == JsonValueKind.Object)
        {
            var ticket = data.Deserialize<ExpoPushTicket>();
            return ticket is null ? [] : [ticket];
        }

        return [];
    }

    private sealed record ExpoPushMessage(
        [property: JsonPropertyName("to")] string To,
        [property: JsonPropertyName("title")] string Title,
        [property: JsonPropertyName("body")] string Body,
        [property: JsonPropertyName("data")] IReadOnlyDictionary<string, string> Data);

    private sealed class ExpoPushResponse
    {
        [JsonPropertyName("data")]
        public JsonElement Data { get; set; }

        [JsonPropertyName("errors")]
        public List<ExpoRequestError>? Errors { get; set; }
    }

    private sealed class ExpoRequestError
    {
        [JsonPropertyName("code")]
        public string? Code { get; set; }
    }

    private sealed class ExpoPushTicket
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("details")]
        public ExpoPushTicketDetails? Details { get; set; }
    }

    private sealed class ExpoPushTicketDetails
    {
        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }
}
