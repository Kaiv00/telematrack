using Azure.Messaging.EventHubs;
using Microsoft.Extensions.Logging;
using TelematrackFunctions;
using Xunit;

namespace FunctionsTests;

// CONTRACT: infra/alerts.tf searches the logs for AlertMessages.CriticalMarker.
// If one of these tests fails, fix the code AND the alert query together.
public class AlertWordingTests
{
    private sealed record LogEntry(LogLevel Level, string Message, string? Template);

    private sealed class CapturingLogger : ILogger<FaultDetector>
    {
        public List<LogEntry> Entries { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter)
        {
            // The unfilled template is stored under the key "{OriginalFormat}"
            var template = (state as IReadOnlyList<KeyValuePair<string, object?>>)
                ?.FirstOrDefault(kv => kv.Key == "{OriginalFormat}").Value as string;

            Entries.Add(new LogEntry(logLevel, formatter(state, exception), template));
        }
    }

    private sealed class FakeRepository : IReadingRepository
    {
        public List<ReadingDocument> Saved { get; } = new();

        public Task SaveAsync(ReadingDocument reading, CancellationToken ct = default)
        {
            Saved.Add(reading);
            return Task.CompletedTask;
        }
    }

    private static EventData Message(string? faultCode, double oilTemp = 90) =>
        new(BinaryData.FromObjectAsJson(new
        {
            vehicleId = "FC-RX7-001",
            timestamp = new DateTime(2026, 10, 22, 18, 46, 22, DateTimeKind.Utc),
            rpm = 4200,
            oilTempCelsius = oilTemp,
            speedKmh = 88,
            faultCode
        }));

    private static async Task<(CapturingLogger Logger, FakeRepository Repo)> RunAsync(
        params EventData[] events)
    {
        var logger = new CapturingLogger();
        var repo = new FakeRepository();
        await new FaultDetector(logger, repo).Run(events, CancellationToken.None);
        return (logger, repo);
    }

    [Fact]
    public async Task CriticalReading_LogsTheTextTheAlertRuleSearchesFor()
    {
        var (logger, _) = await RunAsync(Message("P1A57"));

        var alert = Assert.Single(logger.Entries, e => e.Message.Contains("ALERT"));

        Assert.Equal(LogLevel.Warning, alert.Level);
        Assert.Contains(AlertMessages.CriticalMarker, alert.Message);
        Assert.Contains(AlertMessages.CriticalMarker, alert.Template!);
    }

    [Fact]
    public async Task WarningReading_DoesNotUseTheCriticalText()
    {
        var (logger, _) = await RunAsync(Message("P0300"));

        var alert = Assert.Single(logger.Entries, e => e.Message.Contains("ALERT"));

        Assert.DoesNotContain(AlertMessages.CriticalMarker, alert.Message);
        Assert.DoesNotContain(AlertMessages.CriticalMarker, alert.Template!);
    }

    [Fact]
    public async Task HealthyReading_LogsNoAlert()
    {
        var (logger, _) = await RunAsync(Message(null));

        Assert.DoesNotContain(logger.Entries, e => e.Message.Contains("ALERT"));
    }

    [Fact]
    public async Task EveryReading_IsSaved_WithItsAlertLevel()
    {
        var (_, repo) = await RunAsync(Message("P1A57"), Message("P0300"), Message(null));

        Assert.Equal(new[] { "Critical", "Warning", "None" },
            repo.Saved.Select(r => r.AlertLevel));
    }

    [Fact]
    public async Task MalformedMessage_IsSkipped_AndTheRestOfTheBatchIsStillSaved()
    {
        var garbage = new EventData(BinaryData.FromString("garbage"));

        var (logger, repo) = await RunAsync(Message("P1A57"), garbage, Message(null));

        Assert.Equal(2, repo.Saved.Count);
        Assert.Single(logger.Entries, e => e.Level == LogLevel.Error);
    }

    [Fact]
    public void AlertRuleInTerraform_SearchesForTheSameText()
    {
        // Walk up from the test's folder until we find the repo root (it has an infra/ folder)
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "infra")))
            dir = dir.Parent;

        Assert.NotNull(dir);
        var terraform = File.ReadAllText(Path.Combine(dir!.FullName, "infra", "alerts.tf"));

        Assert.Contains(AlertMessages.CriticalMarker, terraform);
    }
}