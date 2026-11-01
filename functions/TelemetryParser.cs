using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace TelematrackFunctions;

public static class TelemetryParser
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static bool TryParse(string json, [NotNullWhen(true)] out TelemetryPayload? payload)
    {
        payload = null;

        if (string.IsNullOrWhiteSpace(json)) return false;

        try
        {
            var parsed = JsonSerializer.Deserialize<TelemetryPayload>(json, Options);

            if (parsed is null) return false;
            if (string.IsNullOrWhiteSpace(parsed.VehicleId)) return false;
            if (parsed.Timestamp == default) return false;

            payload = parsed;
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}