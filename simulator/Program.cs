using System.Text.Json;
using TelematrackSimulator;

var vehicleIds = new[] { "FC-RX7-001", "AE86-002", "GT-R32-003" };
var generator = new TelemetryGenerator();

var jsonOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = false
};

// Default 60s keeps 3 cars under the free tier's 8,000 messages/day cap
var intervalSeconds =
    int.TryParse(Environment.GetEnvironmentVariable("SIMULATOR_INTERVAL_SECONDS"), out var parsed) && parsed > 0
        ? parsed
        : 60;

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    Console.WriteLine("\nShutting down simulator cleanly...");
    e.Cancel = true;
    cts.Cancel();
};

ITelemetrySink sink;
try
{
    sink = SinkFactory.Create(vehicleIds, jsonOptions);
}
catch (InvalidOperationException ex)
{
    Console.Error.WriteLine($"Configuration error: {ex.Message}");
    return 1;
}

await using var sinkScope = sink;

Console.WriteLine($"Telematrack simulator starting (every {intervalSeconds}s). Press Ctrl+C to stop.");

try
{
    while (!cts.Token.IsCancellationRequested)
    {
        foreach (var vehicleId in vehicleIds)
        {
            var payload = generator.Generate(vehicleId);
            try
            {
                await sink.SendAsync(payload, cts.Token);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // One failed send must not kill the whole fleet
                Console.Error.WriteLine($"Send failed for {vehicleId}: {ex.Message}");
            }
        }

        await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), cts.Token);
    }
}
catch (OperationCanceledException)
{
    // Normal shutdown path
}

Console.WriteLine("Simulator stopped successfully.");
return 0;