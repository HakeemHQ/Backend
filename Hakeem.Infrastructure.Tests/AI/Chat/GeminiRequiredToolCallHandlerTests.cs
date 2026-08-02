using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using Hakeem.Infrastructure.AI.Chat;

namespace Hakeem.Infrastructure.Tests.AI.Chat;

public sealed class GeminiRequiredToolCallHandlerTests
{
    [Fact]
    public async Task SendAsync_InitialToolRequest_ForcesAnyMode()
    {
        var body = await SendAsync(
            """
            {
              "contents": [
                { "role": "user", "parts": [{ "text": "Use the tool." }] }
              ],
              "tools": [
                { "functionDeclarations": [{ "name": "read_document_ocr" }] }
              ]
            }
            """);

        Assert.Equal("ANY", ReadMode(body));
    }

    [Fact]
    public async Task SendAsync_FunctionResultContinuation_DisablesTools()
    {
        var body = await SendAsync(
            """
            {
              "contents": [
                { "role": "model", "parts": [{ "functionCall": { "name": "read_document_ocr" } }] },
                { "role": "user", "parts": [{ "functionResponse": { "name": "read_document_ocr" } }] }
              ],
              "tools": [
                { "functionDeclarations": [{ "name": "read_document_ocr" }] }
              ]
            }
            """);

        Assert.Equal("NONE", ReadMode(body));
    }

    [Fact]
    public async Task SendAsync_NewTurnAfterEarlierFunctionResult_ForcesAnyMode()
    {
        var body = await SendAsync(
            """
            {
              "contents": [
                { "role": "user", "parts": [{ "functionResponse": { "name": "read_document_ocr" } }] },
                { "role": "model", "parts": [{ "text": "OCR received." }] },
                { "role": "user", "parts": [{ "text": "Submit the extraction." }] }
              ],
              "tools": [
                { "functionDeclarations": [{ "name": "submit_extraction" }] }
              ]
            }
            """);

        Assert.Equal("ANY", ReadMode(body));
    }

    [Fact]
    public async Task SendAsync_RequestWithoutFunctionDeclarations_IsUnchanged()
    {
        const string payload =
            """
            {"contents":[{"role":"user","parts":[{"text":"No tools."}]}]}
            """;

        var body = await SendAsync(payload);

        Assert.Equal(payload, body);
    }

    private static string ReadMode(string body)
    {
        var root = JsonNode.Parse(body)!.AsObject();

        return root["toolConfig"]!
            ["functionCallingConfig"]!
            ["mode"]!
            .GetValue<string>();
    }

    private static async Task<string> SendAsync(string payload)
    {
        var recorder = new RecordingHandler();
        using var handler = new GeminiRequiredToolCallHandler
        {
            InnerHandler = recorder
        };
        using var client = new HttpClient(handler);
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "https://generativelanguage.googleapis.com/v1beta/models/test:generateContent")
        {
            Content = new StringContent(
                payload,
                Encoding.UTF8,
                "application/json")
        };

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", recorder.ContentType);
        return recorder.Body!;
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public string? Body { get; private set; }
        public string? ContentType { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Body = await request.Content!.ReadAsStringAsync(
                cancellationToken);
            ContentType = request.Content.Headers.ContentType?.MediaType;

            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}
