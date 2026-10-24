using TelematrackFunctions;
using Xunit;

namespace FunctionsTests;

public class ReadingDocumentTests
{
    private static readonly TelemetryPayload Payload = new(
        VehicleId: "FC-RX7-001",
        Timestamp: new DateTime(2026, 10, 22, 18, 46, 22, DateTimeKind.Utc),
        Rpm: 4200,
        OilTempCelsius: 96.5,
        SpeedKmh: 88,
        FaultCode: "P1A57");

    [Fact]
    public void SameReading_ProducesSameId()
    {
        var a = ReadingDocument.From(Payload, Severity.Critical);
        var b = ReadingDocument.From(Payload, Severity.Critical);

        Assert.Equal(a.Id, b.Id);
    }

    [Fact]
    public void DifferentVehicles_AtSameInstant_ProduceDifferentIds()
    {
        var other = Payload with { VehicleId = "AE86-002" };

        Assert.NotEqual(
            ReadingDocument.From(Payload, Severity.None).Id,
            ReadingDocument.From(other, Severity.None).Id);
    }

    [Fact]
    public void From_CopiesFieldsAndAlertLevel()
    {
        var doc = ReadingDocument.From(Payload, Severity.Critical);

        Assert.Equal("FC-RX7-001", doc.VehicleId);
        Assert.Equal("P1A57", doc.FaultCode);
        Assert.Equal("Critical", doc.AlertLevel);
    }
}