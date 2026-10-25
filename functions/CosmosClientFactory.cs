using Microsoft.Azure.Cosmos;

namespace TelematrackFunctions;

public static class CosmosClientFactory
{
    public static CosmosClient Create(string endpoint, string key)
    {
        var options = new CosmosClientOptions
        {
            // Write C# "VehicleId" as JSON "vehicleId", matching the /vehicleId partition key
            SerializerOptions = new CosmosSerializationOptions
            {
                PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
            }
        };

        // The local emulator needs Gateway mode; real Azure keeps the faster default
        if (new Uri(endpoint).IsLoopback)
        {
            options.ConnectionMode = ConnectionMode.Gateway;
            options.LimitToEndpoint = true;
        }

        return new CosmosClient(endpoint, key, options);
    }
}