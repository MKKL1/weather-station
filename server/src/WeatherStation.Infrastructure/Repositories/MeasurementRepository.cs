using System.Net;
using WeatherStation.Core;
using WeatherStation.Core.Entities;

using Microsoft.Azure.Cosmos;
using WeatherStation.Infrastructure.Cosmos;
using WeatherStation.Shared.Documents;
using WeatherStation.Shared.Cosmos;

namespace WeatherStation.Infrastructure.Repositories;

public class MeasurementRepository(Container viewContainer, CosmosMapper mapper): IMeasurementRepository
{
    public async Task<WeatherReadingEntity?> GetLatest(string deviceId, CancellationToken ct)
    {
        try
        {
            var item = await viewContainer.ReadItemAsync<LatestWeatherDocument>(IdBuilder.BuildLatest(deviceId), 
                new PartitionKey(deviceId), null, ct);

            return item.Resource == null ? null : mapper.ToEntity(item.Resource);
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public Task<IEnumerable<DailyWeatherEntity>> GetRange(string deviceId, DateTime requestStart, DateTime requestEnd, CancellationToken ct)
    {
        throw new NotImplementedException();
    }
}