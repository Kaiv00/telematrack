using TelematrackSimulator.Models;

namespace TelematrackSimulator;

public class TelemetryGenerator
{
    private readonly Random _random = new Random();
    private readonly string[] _faultCodes = 
    { 
        "P0300", 
        "P1A57", 
        "P0171"
    };

    public TelemetryPayload Generate(string vehicleId)
    {
        bool hasFault =  _random.Next(0, 20) == 0;

        return new TelemetryPayload
        {
            VehicleId = vehicleId,
            Timestamp = DateTime.UtcNow,
            Rpm = _random.Next(800, 7000),
            OilTempCelsius = Math.Round(_random.NextDouble() * (120 - 70) + 70, 1),
            SpeedKmh = _random.Next(0, 180),
            FaultCode = hasFault ? _faultCodes[_random.Next(_faultCodes.Length)] : null
        };
    }
}