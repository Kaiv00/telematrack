namespace TelematrackSimulator.Models;

public class TelemetryPayload
{
    public string VehicleId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public int Rpm { get; set; }
    public double OilTempCelsius { get; set; }
    public int SpeedKmh { get; set; }
    public string? FaultCode { get; set; }

}