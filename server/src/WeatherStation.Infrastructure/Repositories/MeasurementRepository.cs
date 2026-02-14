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

    /// <summary>
    /// Minimum day count at which daily requests switch to fetching weekly documents.
    /// </summary>
    private const int WeeklyFetchThreshold = 7;

    public Task<IEnumerable<AggregatedMeasurement>> GetRange(string deviceId,
        HistoryGranularity granularity,
        DateTimeOffset requestStart,
        DateTimeOffset requestEnd,
        CancellationToken ct)
    {
        return granularity switch
        {
            HistoryGranularity.Auto or HistoryGranularity.Daily
                => GetDailyRange(deviceId, requestStart, requestEnd, ct),
            HistoryGranularity.Hourly
                => GetHourlyRange(deviceId, requestStart, requestEnd, ct),
            HistoryGranularity.Weekly
                => GetWeeklyRange(deviceId, requestStart, requestEnd, skipDaily: true, ct),
            _ => throw new ArgumentOutOfRangeException(nameof(granularity), granularity, null)
        };
    }

    public async Task<IEnumerable<AggregatedMeasurement>> GetDailyRange(
        string deviceId,
        DateTimeOffset requestStart,
        DateTimeOffset requestEnd,
        CancellationToken ct)
    {
        var dayCount = requestEnd.Subtract(requestStart).Days + 1;
        var currentWeekStart = GetCurrentIsoWeekStart();
        
        if (dayCount < WeeklyFetchThreshold || requestStart >= currentWeekStart)
            return await GetDailyFromDailyDocuments(deviceId, requestStart, requestEnd, ct);
        
        if (requestEnd < currentWeekStart)
            return await GetDailyFromWeeklyDocuments(deviceId, requestStart, requestEnd, ct);
        
        var pastRangeEnd = currentWeekStart.AddTicks(-1);

        var pastWeeklyTask = GetDailyFromWeeklyDocuments(deviceId, requestStart, pastRangeEnd, ct);
        var currentDailyTask = GetDailyFromDailyDocuments(deviceId, currentWeekStart, requestEnd, ct);

        var results = await Task.WhenAll(pastWeeklyTask, currentDailyTask);
        return results.SelectMany(x => x);
    }

    private static DateTimeOffset GetCurrentIsoWeekStart()
    {
        var today = DateTimeOffset.UtcNow.DateTime;
        var year = ISOWeek.GetYear(today);
        var week = ISOWeek.GetWeekOfYear(today);
        return new DateTimeOffset(ISOWeek.ToDateTime(year, week, DayOfWeek.Monday), TimeSpan.Zero);
    }

    public async Task<IEnumerable<AggregatedMeasurement>> GetDailyFromDailyDocuments(
        string deviceId,
        DateTimeOffset requestStart,
        DateTimeOffset requestEnd,
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
            return items == null
                ? []
                : items.SelectMany(x => AggregatedMeasurementCosmosMapper.ToEntity(x, skipHourly: true, skipDaily: false));
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return [];
        }
    }

    public async Task<IEnumerable<AggregatedMeasurement>> GetDailyFromWeeklyDocuments(
        string deviceId,
        DateTimeOffset requestStart,
        DateTimeOffset requestEnd,
        CancellationToken ct)
    {
        var allData = await GetWeeklyRange(deviceId, requestStart, requestEnd, skipDaily: false, ct);

        var startDate = requestStart.UtcDateTime.Date;
        var endDate = requestEnd.UtcDateTime.Date;

        return allData
            .Where(m => m.Granularity == HistoryGranularity.Daily)
            .Where(m => m.StartTime.UtcDateTime.Date >= startDate && m.StartTime.UtcDateTime.Date <= endDate);
    }

    public async Task<IEnumerable<AggregatedMeasurement>> GetHourlyRange(
        string deviceId,
        DateTimeOffset requestStart,
        DateTimeOffset requestEnd,
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
            return items == null
                ? []
                : items.SelectMany(x => AggregatedMeasurementCosmosMapper.ToEntity(x, skipHourly: false, skipDaily: true));
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return [];
        }
    }

    public async Task<IEnumerable<AggregatedMeasurement>> GetWeeklyRange(string deviceId,
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