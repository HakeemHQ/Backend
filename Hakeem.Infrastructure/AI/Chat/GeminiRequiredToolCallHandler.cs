using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;

namespace Hakeem.Infrastructure.AI.Chat;

internal sealed class GeminiRequiredToolCallHandler : DelegatingHandler
{
    private static readonly TimeSpan CallIdLifetime =
        TimeSpan.FromMinutes(10);

    private readonly ConcurrentDictionary<string, CachedCallId>
        _callIdsByThoughtSignature = new(StringComparer.Ordinal);

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var isToolRequest = false;

        if (request.Content is not null &&
            request.Method == HttpMethod.Post)
        {
            isToolRequest = await ConfigureRequiredToolCallAsync(
                request,
                cancellationToken);
        }

        var response = await base.SendAsync(request, cancellationToken);

        if (isToolRequest && response.IsSuccessStatusCode)
        {
            await CaptureFunctionCallIdsAsync(
                response,
                cancellationToken);
        }

        return response;
    }

    private async Task<bool> ConfigureRequiredToolCallAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var originalContent = request.Content!;
        var payload = await originalContent.ReadAsStringAsync(
            cancellationToken);

        JsonObject? root;

        try
        {
            root = JsonNode.Parse(payload) as JsonObject;
        }
        catch (System.Text.Json.JsonException)
        {
            return false;
        }

        if (root is null || !HasFunctionDeclarations(root["tools"]))
        {
            return false;
        }

        InjectFunctionCallIds(root);

        var mode = LatestContentContainsFunctionResponse(root)
            ? "NONE"
            : "ANY";

        var toolConfig = root["toolConfig"] as JsonObject
            ?? new JsonObject();
        toolConfig["functionCallingConfig"] = new JsonObject
        {
            ["mode"] = mode
        };
        root["toolConfig"] = toolConfig;

        var replacement = new ByteArrayContent(
            Encoding.UTF8.GetBytes(root.ToJsonString()));
        CopyContentHeaders(originalContent.Headers, replacement.Headers);

        request.Content = replacement;
        originalContent.Dispose();

        return true;
    }

    private async Task CaptureFunctionCallIdsAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.Content is null)
        {
            return;
        }

        var originalContent = response.Content;
        var payload = await originalContent.ReadAsStringAsync(
            cancellationToken);

        JsonObject? root;

        try
        {
            root = JsonNode.Parse(payload) as JsonObject;
        }
        catch (System.Text.Json.JsonException)
        {
            return;
        }

        if (root is null)
        {
            return;
        }

        RemoveExpiredCallIds();

        foreach (var part in FindPartObjects(root))
        {
            var functionCall = GetObject(
                part,
                "functionCall",
                "function_call");
            var thoughtSignature = GetString(
                part,
                "thoughtSignature",
                "thought_signature");
            var callId = functionCall is null
                ? null
                : GetString(functionCall, "id", "id");

            if (string.IsNullOrWhiteSpace(thoughtSignature) ||
                string.IsNullOrWhiteSpace(callId))
            {
                continue;
            }

            _callIdsByThoughtSignature[thoughtSignature] =
                new CachedCallId(
                    callId,
                    DateTimeOffset.UtcNow.Add(CallIdLifetime));
        }

        var replacement = new ByteArrayContent(
            Encoding.UTF8.GetBytes(payload));
        CopyContentHeaders(originalContent.Headers, replacement.Headers);
        response.Content = replacement;
        originalContent.Dispose();
    }

    private void InjectFunctionCallIds(JsonObject root)
    {
        RemoveExpiredCallIds();

        if (root["contents"] is not JsonArray contents)
        {
            return;
        }

        var pendingCallIds = new Dictionary<
            string,
            Queue<string>>(StringComparer.Ordinal);

        foreach (var content in contents)
        {
            foreach (var part in FindPartObjects(content))
            {
                var functionCall = GetObject(
                    part,
                    "functionCall",
                    "function_call");

                if (functionCall is not null)
                {
                    var thoughtSignature = GetString(
                        part,
                        "thoughtSignature",
                        "thought_signature");
                    var callId = GetString(functionCall, "id", "id");

                    if (string.IsNullOrWhiteSpace(callId) &&
                        !string.IsNullOrWhiteSpace(thoughtSignature) &&
                        _callIdsByThoughtSignature.TryGetValue(
                            thoughtSignature,
                            out var cachedCallId))
                    {
                        callId = cachedCallId.Id;
                        functionCall["id"] = callId;
                    }

                    var functionName = GetString(
                        functionCall,
                        "name",
                        "name");

                    if (!string.IsNullOrWhiteSpace(functionName) &&
                        !string.IsNullOrWhiteSpace(callId))
                    {
                        if (!pendingCallIds.TryGetValue(
                                functionName,
                                out var callIds))
                        {
                            callIds = new Queue<string>();
                            pendingCallIds[functionName] = callIds;
                        }

                        callIds.Enqueue(callId);
                    }
                }

                var functionResponse = GetObject(
                    part,
                    "functionResponse",
                    "function_response");

                if (functionResponse is null ||
                    !string.IsNullOrWhiteSpace(
                        GetString(functionResponse, "id", "id")))
                {
                    continue;
                }

                var responseFunctionName = GetString(
                    functionResponse,
                    "name",
                    "name");

                if (!string.IsNullOrWhiteSpace(responseFunctionName) &&
                    pendingCallIds.TryGetValue(
                        responseFunctionName,
                        out var responseCallIds) &&
                    responseCallIds.Count > 0)
                {
                    functionResponse["id"] =
                        responseCallIds.Dequeue();
                }
            }
        }
    }

    private void RemoveExpiredCallIds()
    {
        var now = DateTimeOffset.UtcNow;

        foreach (var entry in _callIdsByThoughtSignature)
        {
            if (entry.Value.ExpiresAt <= now)
            {
                _callIdsByThoughtSignature.TryRemove(
                    entry.Key,
                    out _);
            }
        }
    }

    private static IEnumerable<JsonObject> FindPartObjects(
        JsonNode? node)
    {
        if (node is JsonObject jsonObject)
        {
            if (jsonObject.ContainsKey("functionCall") ||
                jsonObject.ContainsKey("function_call") ||
                jsonObject.ContainsKey("functionResponse") ||
                jsonObject.ContainsKey("function_response"))
            {
                yield return jsonObject;
            }

            foreach (var property in jsonObject)
            {
                foreach (var part in FindPartObjects(property.Value))
                {
                    yield return part;
                }
            }
        }
        else if (node is JsonArray jsonArray)
        {
            foreach (var item in jsonArray)
            {
                foreach (var part in FindPartObjects(item))
                {
                    yield return part;
                }
            }
        }
    }

    private static JsonObject? GetObject(
        JsonObject parent,
        string camelCaseName,
        string snakeCaseName)
    {
        return parent[camelCaseName] as JsonObject ??
               parent[snakeCaseName] as JsonObject;
    }

    private static string? GetString(
        JsonObject parent,
        string camelCaseName,
        string snakeCaseName)
    {
        var node = parent[camelCaseName] ?? parent[snakeCaseName];

        return node is JsonValue value &&
               value.TryGetValue<string>(out var result)
            ? result
            : null;
    }

    private static bool HasFunctionDeclarations(JsonNode? tools)
    {
        if (tools is not JsonArray toolArray)
        {
            return false;
        }

        return toolArray
            .OfType<JsonObject>()
            .Any(tool =>
                tool["functionDeclarations"] is JsonArray
                {
                    Count: > 0
                } ||
                tool["function_declarations"] is JsonArray
                {
                    Count: > 0
                });
    }

    private static bool LatestContentContainsFunctionResponse(
        JsonObject root)
    {
        if (root["contents"] is not JsonArray contents ||
            contents.Count == 0)
        {
            return false;
        }

        return ContainsFunctionResponse(contents[^1]);
    }

    private static bool ContainsFunctionResponse(JsonNode? node)
    {
        return node switch
        {
            JsonObject jsonObject =>
                jsonObject.ContainsKey("functionResponse") ||
                jsonObject.ContainsKey("function_response") ||
                jsonObject.Any(property =>
                    ContainsFunctionResponse(property.Value)),
            JsonArray jsonArray => jsonArray.Any(
                ContainsFunctionResponse),
            _ => false
        };
    }

    private static void CopyContentHeaders(
        HttpContentHeaders source,
        HttpContentHeaders destination)
    {
        foreach (var header in source)
        {
            if (header.Key.Equals(
                    "Content-Length",
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            destination.TryAddWithoutValidation(
                header.Key,
                header.Value);
        }
    }

    private readonly record struct CachedCallId(
        string Id,
        DateTimeOffset ExpiresAt);
}
