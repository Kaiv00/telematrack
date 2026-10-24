using System.Text.Json;
using Azure.Messaging.EventHubs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace TelematrackFunctions;

public class FaultDetector
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly ILogger<FaultDetector> _logger;
    private readonly IReadingRepository _repository;

    public FaultDetector(ILogger<FaultDetector> logger, IReadingRepository repository)
    {
        _logger = logger;
        _repository = repository;
    }

    [Function("FaultDetector")]
    public async Task Run(
        [EventHubTrigger("%IoTHubEventPath%",
            Connection = "IoTHubConnection",
            ConsumerGroup = "%ConsumerGroup%")] EventData[] events,
        CancellationToken cancellationToken)
    {
        foreach (var e in events)
        {
            var payload = JsonSerializer.Deserialize<TelemetryPayload>(
                e.EventBody.ToString(), JsonOptions);

            if (payload is null) continue;

            var severity = FaultEvaluator.Evaluate(payload);

            if (severity != Severity.None)
                _logger.LogWarning(
                    "ALERT {Severity} on {VehicleId}: code={FaultCode}, oilTemp={Oil}",
                    severity, payload.VehicleId, payload.FaultCode, payload.OilTempCelsius);

            await _repository.SaveAsync(
                ReadingDocument.From(payload, severity), cancellationToken);
        }
    }
}