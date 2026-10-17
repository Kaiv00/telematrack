using TelematrackFunctions;
using Xunit;

namespace FunctionsTests;

public class FaultEvaluatorTests
{
    // Helper: a healthy reading by default, so each test only changes
    // the one thing it cares about.
    private static TelemetryPayload MakePayload(
        string? faultCode = null,
        double oilTemp = 90) =>
        new(
            VehicleId: "TEST-001",
            Timestamp: DateTime.UtcNow,
            Rpm: 3000,
            OilTempCelsius: oilTemp,
            SpeedKmh: 80,
            FaultCode: faultCode);

    [Fact]
    public void CleanReading_ReturnsNone()
    {
        // Arrange
        var payload = MakePayload();

        // Act
        var result = FaultEvaluator.Evaluate(payload);

        // Assert
        Assert.Equal(Severity.None, result);
    }

    [Fact]
    public void SimulatedApexSealCode_ReturnsCritical()
    {
        var result = FaultEvaluator.Evaluate(MakePayload(faultCode: "P1A57"));

        Assert.Equal(Severity.Critical, result);
    }

    [Theory]
    [InlineData("P0300")]
    [InlineData("P0171")]
    public void KnownMinorFaultCodes_ReturnWarning(string code)
    {
        var result = FaultEvaluator.Evaluate(MakePayload(faultCode: code));

        Assert.Equal(Severity.Warning, result);
    }

    [Fact]
    public void HighOilTemp_ReturnsWarning()
    {
        var result = FaultEvaluator.Evaluate(MakePayload(oilTemp: 120));

        Assert.Equal(Severity.Warning, result);
    }

    [Fact]
    public void OilTempExactlyAtThreshold_ReturnsNone()
    {
        // The rule is "> 115", so 115 itself must NOT alert.
        var result = FaultEvaluator.Evaluate(MakePayload(oilTemp: 115));

        Assert.Equal(Severity.None, result);
    }

    [Fact]
    public void CriticalFault_WinsOverHighOilTemp()
    {
        // Two problems at once: the worse one should be reported.
        var result = FaultEvaluator.Evaluate(
            MakePayload(faultCode: "P1A57", oilTemp: 130));

        Assert.Equal(Severity.Critical, result);
    }

    [Fact]
public void UnknownFaultCode_ReturnsWarning()
{
    // A code we've never heard of must never be silently ignored.
    var result = FaultEvaluator.Evaluate(MakePayload(faultCode: "P9999"));

    Assert.Equal(Severity.Warning, result);
}

[Theory]
[InlineData("")]
[InlineData("   ")]
public void BlankFaultCode_IsTreatedAsNoFault(string code)
{
    // An empty string is "no fault", not an unknown fault.
    var result = FaultEvaluator.Evaluate(MakePayload(faultCode: code));

    Assert.Equal(Severity.None, result);
}
}