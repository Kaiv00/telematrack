namespace TelematrackFunctions;

public record TelemetryPayload(
    string VehicleId,
    DateTime Timestamp,
    int Rpm,
    double OilTempCelsius,
    int SpeedKmh,
    string? FaultCode);

public enum Severity { None, Warning, Critical }

public static class FaultEvaluator
{
    private static readonly Dictionary<string, Severity> KnownFaults = new()
    {
        ["P0300"] = Severity.Warning,   // random misfire
        ["P0171"] = Severity.Warning,   // system too lean
        ["P1A57"] = Severity.Critical,  // SIMULATED rotary apex seal warning
    };

    public static Severity Evaluate(TelemetryPayload payload)
{
    if (!string.IsNullOrWhiteSpace(payload.FaultCode))
    {
        return KnownFaults.TryGetValue(payload.FaultCode, out var severity)
            ? severity
            : Severity.Warning; // unknown code: fail safe, never ignore
    }


        if (payload.OilTempCelsius > 115) return Severity.Warning;

        return Severity.None;
    }
}