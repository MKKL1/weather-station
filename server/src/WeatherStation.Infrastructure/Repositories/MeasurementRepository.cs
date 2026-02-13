using System.Globalization;
using System.Net;
using WeatherStation.Core;
using WeatherStation.Core.Entities;

using Microsoft.Azure.Cosmos;
using WeatherStation.Infrastructure.Cosmos;
using WeatherStation.Shared.Documents;
using WeatherStation.Shared.Cosmos;

namespace WeatherStation.Infrastructure.Repositories;

public class MeasurementRepository(Container viewContainer): IMeasurementRepository
{
    public async Task<LatestMeasurement?> GetLatest(string deviceId, CancellationToken ct)
    {
        try
        {
            var item = await viewContainer.ReadItemAsync<LatestWeatherDocument>(IdBuilder.BuildLatest(deviceId), 
                new PartitionKey(deviceId), null, ct);

            return item.Resource == null ? null : LatestMeasurementCosmosMapper.ToEntity(item.Resource);
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public Task<IEnumerable<AggregatedMeasurement>> GetRange(string deviceId,
        HistoryGranularity granularity,
        DateTimeOffset requestStart,
        DateTimeOffset requestEnd,
        CancellationToken ct)
    {
        //TODO for now it's simple switch, but there is big optimization possible.
        //Weekly granularity provides daily data as well.
        //It could be used to make large queries much more efficient.

        return granularity switch
        {
            HistoryGranularity.Auto or HistoryGranularity.Daily => GetRange(deviceId, requestStart, requestEnd, true,
                false, ct),
            HistoryGranularity.Hourly => GetRange(deviceId, requestStart, requestEnd, false, true, ct),
            HistoryGranularity.Weekly => GetWeeklyRange(deviceId, requestStart, requestEnd, true, ct),
            _ => throw new ArgumentOutOfRangeException(nameof(granularity), granularity, null)
        };
    }

    private async Task<IEnumerable<AggregatedMeasurement>> GetRange(
        string deviceId, 
        DateTimeOffset requestStart, 
        DateTimeOffset requestEnd,
        bool skipHourly,
        bool skipDaily,
        CancellationToken ct)
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
            return items == null ? [] : items.SelectMany(x => AggregatedMeasurementCosmosMapper.ToEntity(x, skipHourly, skipDaily));
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return [];
        }
    }

    private async Task<IEnumerable<AggregatedMeasurement>> GetWeeklyRange(string deviceId,
        DateTimeOffset requestStart,
        DateTimeOffset requestEnd,
        bool skipDaily,
        CancellationToken ct)
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
            return items == null ? [] : items.SelectMany(x => AggregatedMeasurementCosmosMapper.ToEntity(x, skipDaily));
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return [];
        }
    }
}