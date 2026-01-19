using System.Globalization;
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

    public async Task<IEnumerable<DailyWeatherEntity>> GetRange(string deviceId, DateTime requestStart, DateTime requestEnd, CancellationToken ct)
    {
        var partitionKey = new PartitionKey(deviceId);
        var itemsToFetch = Enumerable.Range(0, requestEnd.Subtract(requestStart).Days + 1)
            .Select(offset => requestStart.AddDays(offset))
            .Select(day => GetIsoWeek(day))
            .Select(x => IdBuilder.BuildWeekly(deviceId, x.Year, x.Week))
            .Select(id => (id, partitionKey))
            .ToList();
        
        try
        {
            var items = await viewContainer.ReadManyItemsAsync<DailyWeatherDocument>(itemsToFetch, null, ct);
            return items == null ? [] : items.Select(mapper.ToEntity);
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return [];
        }
    }
    
    private static (int Year, int Week) GetIsoWeek(DateTimeOffset date)
    {
        return (ISOWeek.GetYear(date.DateTime), ISOWeek.GetWeekOfYear(date.DateTime));
    }
}