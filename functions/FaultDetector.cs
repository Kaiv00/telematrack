using Azure.Messaging.EventHubs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace TelematrackFunctions;

public class FaultDetector
{
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
            if (!TelemetryParser.TryParse(e.EventBody.ToString(), out var payload))
            {
                // Log the position, not the body: a bad message could be huge or sensitive
                _logger.LogError("Skipping malformed message (sequence number {Sequence})", e.SequenceNumber);
                continue;
            }

    var severity = FaultEvaluator.Evaluate(payload);
    // ...the rest stays exactly as it is

           if (severity == Severity.Critical)
            _logger.LogWarning(AlertMessages.CriticalTemplate,
            payload.VehicleId, payload.FaultCode, payload.OilTempCelsius);

           else if (severity == Severity.Warning)
            _logger.LogWarning(AlertMessages.WarningTemplate,
            payload.VehicleId, payload.FaultCode, payload.OilTempCelsius);

            await _repository.SaveAsync(
            ReadingDocument.From(payload, severity), cancellationToken);
        }
    }
}