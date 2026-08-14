using System.Net;
using System.Text;
using System.Text.Json;
using Hakeem.Application.Interfaces.Notifications;
using Hakeem.Infrastructure.Services;

namespace Hakeem.Infrastructure.Tests.Notifications;

public sealed class ExpoPushNotificationServiceTests
{
    [Fact]
    public async Task SendAsync_PostsSafePayloadForAllTokensAndReturnsUnregisteredTokens()
    {
        string? requestJson = null;
        var handler = new StubHttpMessageHandler(async (request, cancellationToken) =>
        {
            requestJson = await request.Content!.ReadAsStringAsync(cancellationToken);
            return JsonResponse(
                """
                {
                  "data": [
                    { "status": "ok", "id": "receipt-one" },
                    {
                      "status": "error",
                      "message": "not registered",
                      "details": { "error": "DeviceNotRegistered" }
                    }
                  ]
                }
                """);
        });
        var service = CreateService(handler);
        var requestId = Guid.NewGuid();

        var result = await service.SendAsync(
            CreateMessage(
                ["ExponentPushToken[first]", "ExpoPushToken[second]"],
                requestId),
            CancellationToken.None);

        Assert.Equal(["ExpoPushToken[second]"], result.InactiveDeviceTokens);
        Assert.Equal(
            new Uri("https://exp.host/--/api/v2/push/send"),
            handler.LastRequestUri);

        using var document = JsonDocument.Parse(requestJson!);
        var messages = document.RootElement;
        Assert.Equal(2, messages.GetArrayLength());
        foreach (var message in messages.EnumerateArray())
        {
            Assert.Equal("Doctor access request", message.GetProperty("title").GetString());
            Assert.Equal(
                "A doctor is requesting temporary access to your Hakeem records. " +
                "Open Hakeem to review the request.",
                message.GetProperty("body").GetString());
            var data = message.GetProperty("data");
            Assert.Equal("PatientAccessRequest", data.GetProperty("type").GetString());
            Assert.Equal(requestId.ToString(), data.GetProperty("requestId").GetString());
            Assert.Equal(2, data.EnumerateObject().Count());
        }

        Assert.DoesNotContain("NationalId", requestJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PatientCode", requestJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("accessCode", requestJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("medical", requestJson, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task SendAsync_WhenHttpFailureIsTransient_ThrowsForOutboxRetry(
        HttpStatusCode statusCode)
    {
        var service = CreateService(new StubHttpMessageHandler(
            (_, _) => Task.FromResult(new HttpResponseMessage(statusCode))));

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() =>
            service.SendAsync(
                CreateMessage(["ExponentPushToken[first]"], Guid.NewGuid()),
                CancellationToken.None));

        Assert.Equal(statusCode, exception.StatusCode);
    }

    [Fact]
    public async Task SendAsync_WhenNetworkFails_ThrowsForOutboxRetry()
    {
        var expected = new HttpRequestException("network unavailable");
        var service = CreateService(new StubHttpMessageHandler(
            (_, _) => Task.FromException<HttpResponseMessage>(expected)));

        var actual = await Assert.ThrowsAsync<HttpRequestException>(() =>
            service.SendAsync(
                CreateMessage(["ExponentPushToken[first]"], Guid.NewGuid()),
                CancellationToken.None));

        Assert.Same(expected, actual);
    }

    [Fact]
    public async Task SendAsync_WhenTicketIsRateLimited_ThrowsForOutboxRetry()
    {
        var service = CreateService(new StubHttpMessageHandler(
            (_, _) => Task.FromResult(JsonResponse(
                """
                {
                  "data": [{
                    "status": "error",
                    "details": { "error": "MessageRateExceeded" }
                  }]
                }
                """))));

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            service.SendAsync(
                CreateMessage(["ExponentPushToken[first]"], Guid.NewGuid()),
                CancellationToken.None));
    }

    [Fact]
    public async Task SendAsync_MoreThanOneHundredTokens_SplitsIntoExpoSizedBatches()
    {
        var batchSizes = new List<int>();
        var handler = new StubHttpMessageHandler(async (request, cancellationToken) =>
        {
            var json = await request.Content!.ReadAsStringAsync(cancellationToken);
            using var document = JsonDocument.Parse(json);
            var batchSize = document.RootElement.GetArrayLength();
            batchSizes.Add(batchSize);

            var tickets = string.Join(
                ',',
                Enumerable.Repeat("{\"status\":\"ok\",\"id\":\"receipt\"}", batchSize));
            return JsonResponse($"{{\"data\":[{tickets}]}}");
        });
        var service = CreateService(handler);
        var tokens = Enumerable.Range(0, 101)
            .Select(index => $"ExponentPushToken[token-{index}]")
            .ToArray();

        await service.SendAsync(
            CreateMessage(tokens, Guid.NewGuid()),
            CancellationToken.None);

        Assert.Equal([100, 1], batchSizes);
    }

    private static ExpoPushNotificationService CreateService(
        HttpMessageHandler handler)
    {
        return new ExpoPushNotificationService(new HttpClient(handler)
        {
            BaseAddress = new Uri("https://exp.host/--/api/v2/push/")
        });
    }

    private static PushNotificationMessage CreateMessage(
        IReadOnlyCollection<string> tokens,
        Guid requestId)
    {
        return new PushNotificationMessage(
            tokens,
            "Doctor access request",
            "A doctor is requesting temporary access to your Hakeem records. " +
            "Open Hakeem to review the request.",
            new Dictionary<string, string>
            {
                ["type"] = "PatientAccessRequest",
                ["requestId"] = requestId.ToString()
            });
    }

    private static HttpResponseMessage JsonResponse(string json)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        : HttpMessageHandler
    {
        public Uri? LastRequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequestUri = request.RequestUri;
            return handler(request, cancellationToken);
        }
    }
}
