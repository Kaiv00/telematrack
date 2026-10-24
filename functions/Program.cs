using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;
using TelematrackFunctions;

var builder = FunctionsApplication.CreateBuilder(args);

builder.Services.AddSingleton<IReadingRepository>(_ =>
{
    var endpoint = Environment.GetEnvironmentVariable("CosmosEndpoint")
        ?? throw new InvalidOperationException("CosmosEndpoint is not set");
    var key = Environment.GetEnvironmentVariable("CosmosKey")
        ?? throw new InvalidOperationException("CosmosKey is not set");

    var client = CosmosClientFactory.Create(endpoint, key);
    return new CosmosReadingRepository(client.GetContainer("telematrack", "readings"));
});

builder.ConfigureFunctionsWebApplication();

if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING")))
{
    builder.Services.AddOpenTelemetry()
        .UseFunctionsWorkerDefaults()
        .UseAzureMonitorExporter();
}

builder.Build().Run();
