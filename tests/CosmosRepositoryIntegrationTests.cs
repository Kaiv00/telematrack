using Microsoft.Azure.Cosmos;
using TelematrackFunctions;
using Xunit;

namespace FunctionsTests;

public class CosmosRepositoryIntegrationTests
{
    private const string Endpoint = "http://localhost:8081";
    // The emulator's public, documented development key (not a secret)
    private const string Key = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";

    private static async Task<(CosmosClient Client, Container Container)> SetUpAsync()
    {
        var client = CosmosClientFactory.Create(Endpoint, Key);
        var database = (await client.CreateDatabaseIfNotExistsAsync("telematrack-tests")).Database;
        var container = (await database.CreateContainerIfNotExistsAsync("readings", "/vehicleId")).Container;
        return (client, container);
    }
    
    private static async Task CleanUpAsync(Container container, ReadingDocument reading)
{
    try
    {
        await container.DeleteItemAsync<ReadingDocument>(
            reading.Id, new PartitionKey(reading.VehicleId));
    }
    catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
    {
        // Nothing to clean up: the save never succeeded
    }
}

    // Unique vehicle per test run, so runs never collide
    private static ReadingDocument NewReading() => ReadingDocument.From(
        new TelemetryPayload(
            VehicleId: $"TEST-{Guid.NewGuid():N}",
            Timestamp: DateTime.UtcNow,
            Rpm: 4200,
            OilTempCelsius: 96.5,
            SpeedKmh: 88,
            FaultCode: "P1A57"),
        Severity.Critical);

    [EmulatorFact]
    public async Task SavedReading_CanBeReadBackFromItsPartition()
    {
        var (client, container) = await SetUpAsync();
        using (client)
        {
            var reading = NewReading();
            var key = new PartitionKey(reading.VehicleId);

            try
            {
                await new CosmosReadingRepository(container).SaveAsync(reading);

                var stored = (await container.ReadItemAsync<ReadingDocument>(reading.Id, key)).Resource;

                Assert.Equal(reading.VehicleId, stored.VehicleId);
                Assert.Equal("P1A57", stored.FaultCode);
                Assert.Equal("Critical", stored.AlertLevel);
            }
            finally
            {
                await CleanUpAsync(container, reading);
            }
        }
    }

    [EmulatorFact]
    public async Task SavingSameReadingTwice_StoresOneDocument()
    {
        var (client, container) = await SetUpAsync();
        using (client)
        {
            var reading = NewReading();
            var key = new PartitionKey(reading.VehicleId);
            var repository = new CosmosReadingRepository(container);

            try
            {
                await repository.SaveAsync(reading);
                await repository.SaveAsync(reading); // simulates a redelivered message

                var query = new QueryDefinition("SELECT * FROM c WHERE c.vehicleId = @v")
                    .WithParameter("@v", reading.VehicleId);

                using var iterator = container.GetItemQueryIterator<ReadingDocument>(
                    query, requestOptions: new QueryRequestOptions { PartitionKey = key });

                var count = 0;
                while (iterator.HasMoreResults)
                    count += (await iterator.ReadNextAsync()).Count;

                Assert.Equal(1, count);
            }
            finally
            {
                await CleanUpAsync(container, reading);
            }
        }
    }
}