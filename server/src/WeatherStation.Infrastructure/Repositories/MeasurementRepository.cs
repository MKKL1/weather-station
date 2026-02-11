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

    public async Task<IEnumerable<DailyWeatherEntity>> GetRange(string deviceId, DateTimeOffset requestStart, DateTimeOffset requestEnd, CancellationToken ct)
    {
        var partitionKey = new PartitionKey(deviceId);
        var itemsToFetch = Enumerable.Range(0, requestEnd.Subtract(requestStart).Days + 1)
            .Select(offset => requestStart.AddDays(offset))
            .Select(x => IdBuilder.BuildDaily(deviceId, x))
            .Select(id => (id, partitionKey))
            .ToList();
        
        if (itemsToFetch.Count == 0) return [];
        
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

    public async Task<IEnumerable<WeeklyWeatherEntity>> GetWeeklyRange(string deviceId, DateTimeOffset requestStart, DateTimeOffset requestEnd, CancellationToken ct)
    {
        var partitionKey = new PartitionKey(deviceId);
        var weeksToFetch = new HashSet<(int Year, int Week)>();
        
        for (var date = requestStart; date <= requestEnd; date = date.AddDays(1))
        {
            int year = ISOWeek.GetYear(date.DateTime);
            int week = ISOWeek.GetWeekOfYear(date.DateTime);
            weeksToFetch.Add((year, week));
        }
        var itemsToFetch = weeksToFetch
            .Select(w => IdBuilder.BuildWeekly(deviceId, w.Year, w.Week)) 
            .Select(id => (id, partitionKey))
            .ToList();

        if (itemsToFetch.Count == 0) return [];

        try
        {
            var items = await viewContainer.ReadManyItemsAsync<WeeklyWeatherDocument>(itemsToFetch, null, ct);
            return items == null ? [] : items.Select(mapper.ToEntity);
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return [];
        }
    }
}