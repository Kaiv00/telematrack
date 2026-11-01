using TelematrackFunctions;
using Xunit;

namespace FunctionsTests;

public class TelemetryParserTests
{
    private const string Valid =
        """{"vehicleId":"FC-RX7-001","timestamp":"2026-10-22T18:46:22Z","rpm":4200,"oilTempCelsius":96.5,"speedKmh":88,"faultCode":null}""";

    [Fact]
    public void ValidMessage_IsParsed()
    {
        Assert.True(TelemetryParser.TryParse(Valid, out var payload));
        Assert.Equal("FC-RX7-001", payload.VehicleId);
        Assert.Equal(4200, payload.Rpm);
        Assert.Null(payload.FaultCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not json at all")]
    [InlineData("{}")]
    public void BrokenOrEmptyMessages_AreRejected(string json)
    {
        Assert.False(TelemetryParser.TryParse(json, out var payload));
        Assert.Null(payload);
    }

    [Fact]
    public void MissingTimestamp_IsRejected()
    {
        var json = """{"vehicleId":"FC-RX7-001","rpm":4200,"oilTempCelsius":96.5,"speedKmh":88}""";

        Assert.False(TelemetryParser.TryParse(json, out _));
    }

    [Fact]
    public void BlankVehicleId_IsRejected()
    {
        var json = """{"vehicleId":" ","timestamp":"2026-10-22T18:46:22Z","rpm":1,"oilTempCelsius":90,"speedKmh":1}""";

        Assert.False(TelemetryParser.TryParse(json, out _));
    }
}