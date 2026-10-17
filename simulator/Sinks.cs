using System.Text;
using System.Text.Json;
using Microsoft.Azure.Devices.Client;
using TelematrackSimulator.Models;

namespace TelematrackSimulator;

public interface ITelemetrySink : IAsyncDisposable
{
    Task SendAsync(TelemetryPayload payload, CancellationToken ct);
}

// Dry-run: just prints the JSON
public sealed class ConsoleSink : ITelemetrySink
{
    private readonly JsonSerializerOptions _options;
    public ConsoleSink(JsonSerializerOptions options) => _options = options;

    public Task SendAsync(TelemetryPayload payload, CancellationToken ct)
    {
        Console.WriteLine(JsonSerializer.Serialize(payload, _options));
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

// Real mode: one DeviceClient per car, each with its OWN credentials
public sealed class IoTHubSink : ITelemetrySink
{
    private readonly Dictionary<string, DeviceClient> _clients;
    private readonly JsonSerializerOptions _options;

    public IoTHubSink(Dictionary<string, DeviceClient> clients, JsonSerializerOptions options)
    {
        _clients = clients;
        _options = options;
    }

    public async Task SendAsync(TelemetryPayload payload, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(payload, _options);

        using var message = new Message(Encoding.UTF8.GetBytes(json))
        {
            ContentType = "application/json",
            ContentEncoding = "utf-8"
        };

        await _clients[payload.VehicleId].SendEventAsync(message, ct);
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var client in _clients.Values)
        {
            await client.CloseAsync();
            client.Dispose();
        }
    }
}

public static class SinkFactory
{
    public static ITelemetrySink Create(string[] vehicleIds, JsonSerializerOptions options)
{
    string EnvName(string id) => "IOTHUB_CS_" + id.Replace('-', '_');

    // Pass 1: only look at configuration, create nothing yet
    var missing = vehicleIds
        .Where(id => string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(EnvName(id))))
        .ToList();

    if (missing.Count == vehicleIds.Length)
    {
        Console.WriteLine("No device connection strings found: DRY-RUN mode (printing to console).");
        return new ConsoleSink(options);
    }

    if (missing.Count > 0)
        throw new InvalidOperationException(
            "Missing connection strings for: " + string.Join(", ", missing.Select(EnvName)));

    // Pass 2: everything is present, now build the clients
    var clients = new Dictionary<string, DeviceClient>();
    foreach (var id in vehicleIds)
    {
        try
        {
            clients[id] = DeviceClient.CreateFromConnectionString(
                Environment.GetEnvironmentVariable(EnvName(id))!, TransportType.Mqtt);
        }
        catch (FormatException)
        {
            // Name the variable, never print its value (it's a secret)
            throw new InvalidOperationException($"{EnvName(id)} is not a valid device connection string.");
        }
    }

    return new IoTHubSink(clients, options);
}
}