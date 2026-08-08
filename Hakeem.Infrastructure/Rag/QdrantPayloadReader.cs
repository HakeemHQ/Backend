using Google.Protobuf.Collections;
using Qdrant.Client.Grpc;

namespace Hakeem.Infrastructure.Rag;

internal static class QdrantPayloadReader
{
    public static string? GetString(MapField<string, Value> payload, string key)
    {
        if (!payload.TryGetValue(key, out var value))
        {
            return null;
        }

        return value.KindCase switch
        {
            Value.KindOneofCase.StringValue => value.StringValue,
            Value.KindOneofCase.IntegerValue => value.IntegerValue.ToString(),
            Value.KindOneofCase.DoubleValue => value.DoubleValue.ToString(),
            Value.KindOneofCase.BoolValue => value.BoolValue.ToString(),
            _ => value.ToString()
        };
    }

    public static Guid GetGuid(MapField<string, Value> payload, string key)
    {
        var value = GetString(payload, key);
        return Guid.TryParse(value, out var parsed)
            ? parsed
            : Guid.Empty;
    }

    public static DateTime? GetDateTime(MapField<string, Value> payload, string key)
    {
        var value = GetString(payload, key);
        return DateTime.TryParse(value, out var parsed)
            ? parsed
            : null;
    }

    public static Guid ParsePointId(PointId pointId)
    {
        return pointId.HasUuid
            ? Guid.Parse(pointId.Uuid)
            : Guid.Empty;
    }
}
