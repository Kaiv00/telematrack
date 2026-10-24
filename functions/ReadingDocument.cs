namespace TelematrackFunctions;

public record ReadingDocument(
    string Id,
    string VehicleId,
    DateTime Timestamp,
    int Rpm,
    double OilTempCelsius,
    int SpeedKmh,
    string? FaultCode,
    string AlertLevel)
{
    public static ReadingDocument From(TelemetryPayload p, Severity severity) => new(
        Id: $"{p.VehicleId}_{p.Timestamp.Ticks}",
        VehicleId: p.VehicleId,
        Timestamp: p.Timestamp,
        Rpm: p.Rpm,
        OilTempCelsius: p.OilTempCelsius,
        SpeedKmh: p.SpeedKmh,
        FaultCode: p.FaultCode,
        AlertLevel: severity.ToString());
}