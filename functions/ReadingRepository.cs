using Microsoft.Azure.Cosmos;

namespace TelematrackFunctions;

public interface IReadingRepository
{
    Task SaveAsync(ReadingDocument reading, CancellationToken ct = default);
}

public sealed class CosmosReadingRepository : IReadingRepository
{
    private readonly Container _container;

    public CosmosReadingRepository(Container container) => _container = container;

    public async Task SaveAsync(ReadingDocument reading, CancellationToken ct = default)
    {
        await _container.UpsertItemAsync(
            reading,
            new PartitionKey(reading.VehicleId),
            cancellationToken: ct);
    }
}