using System.Net;
using Microsoft.Azure.Cosmos;
using Worker.Domain;
using Worker.Domain.Models;
using Worker.Dto;
using Worker.Infrastructure.Documents;

namespace Worker.Infrastructure;

public class CosmosWeatherRepository(
    WeatherViewsContainer viewsWrapper,
    RawTelemetryContainer rawWrapper,
    DocumentMapper mapper)
    : IWeatherRepository
{
    private readonly Container _viewsContainer = viewsWrapper.Instance;
    private readonly Container _rawContainer = rawWrapper.Instance;
    
    public async Task SaveRaw(ValidatedTelemetryDto telemetry, string deviceId)
    {
        var document = mapper.ToRawDocument(telemetry, deviceId);
        await _rawContainer.UpsertItemAsync(document, new PartitionKey(deviceId));
    }

    public async Task<List<DailyWeather>> GetManyDaily(string deviceId, IEnumerable<DateTimeOffset> dates)
    {
        var ids = dates.Select(date => 
            (id: IdBuilder.BuildDaily(deviceId, date), pk: new PartitionKey(deviceId))
        ).ToList();

        if (ids.Count == 0) return [];

        var response = await _viewsContainer.ReadManyItemsAsync<DailyWeatherDocument>(ids);
        return response.Resource.Select(mapper.ToDomain).ToList();
    }

    public async Task SaveState(WeatherStateUpdate weatherStateUpdate)
    {
        var deviceId = weatherStateUpdate.CurrentReading.DeviceId;
        var batch = _viewsContainer.CreateTransactionalBatch(new PartitionKey(deviceId));

        batch.UpsertItem(mapper.ToDocument(weatherStateUpdate.CurrentReading));
        foreach (var daily in weatherStateUpdate.DailyChanges) 
        {
            batch.UpsertItem(mapper.ToDocument(daily));
        }

        using var response = await batch.ExecuteAsync();
        if (!response.IsSuccessStatusCode) 
            throw new Exception($"Failed to save weather batch for {deviceId}: {response.ErrorMessage}");
    }
    
    public async Task<(List<DailyWeather> Items, string? ContinuationToken)> GetUnfinalizedBatch(
        DateTimeOffset cutoff, 
        int limit, 
        string? continuationToken)
    {
        var query = new QueryDefinition(
            @"SELECT * FROM c 
          WHERE c.typ = 'daily' 
            AND c.dat.fin = false 
            AND c.dayTs < @cutoff
          ORDER BY c.dayTs ASC"
        ).WithParameter("@cutoff", cutoff.ToUnixTimeSeconds());

        var iterator = _viewsContainer.GetItemQueryIterator<DailyWeatherDocument>(
            query,
            continuationToken,
            new QueryRequestOptions 
            { 
                MaxItemCount = limit,
                MaxConcurrency = -1
            }
        );

        if (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            var items = response.Select(mapper.ToDomain).ToList();
            return (items, response.ContinuationToken);
        }

        return ([], null);
    }

    public async Task SaveDailyBatch(IEnumerable<DailyWeather> dailies)
    {
        var tasks = dailies.Select(async day =>
        {
            try
            {
                var doc = mapper.ToDocument(day);
                var options = new ItemRequestOptions();
                
                if (!string.IsNullOrEmpty(day.Version))
                {
                    options.IfMatchEtag = day.Version;
                }

                await _viewsContainer.UpsertItemAsync(doc, new PartitionKey(day.DeviceId), options);
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.PreconditionFailed)
            {
                // Concurrency conflict: The day was updated by Ingestion while we were finalizing.
                // Action: Ignore. Leave it Unfinalized. It will be picked up next run.
            }
        });

        await Task.WhenAll(tasks);
    }
    
    public async Task<WeeklyWeather?> GetWeekly(string deviceId, int year, int week)
    {
        try
        {
            var id = IdBuilder.BuildWeekly(deviceId, year, week);
            var response = await _viewsContainer.ReadItemAsync<WeeklyWeatherDocument>(
                id, new PartitionKey(deviceId));
            
            return mapper.ToDomain(response.Resource);
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<List<WeeklyWeather>> GetManyWeekly(IEnumerable<(string DeviceId, int Year, int Week)> keys)
    {
        var keyList = keys.ToList();
        if (keyList.Count == 0) return [];

        var ids = keyList.Select(k => 
            (id: IdBuilder.BuildWeekly(k.DeviceId, k.Year, k.Week), pk: new PartitionKey(k.DeviceId))
        ).ToList();

        var response = await _viewsContainer.ReadManyItemsAsync<WeeklyWeatherDocument>(ids);
        return response.Resource.Select(mapper.ToDomain).ToList();
    }

    public async Task SaveWeekly(WeeklyWeather weekly)
    {
        var doc = mapper.ToDocument(weekly);
        var pk = new PartitionKey(weekly.DeviceId);

        if (string.IsNullOrEmpty(weekly.Version))
        {
            await _viewsContainer.CreateItemAsync(doc, pk);
        }
        else
        {
            await _viewsContainer.UpsertItemAsync(doc, pk, new ItemRequestOptions 
            { 
                IfMatchEtag = weekly.Version 
            });
        }
    }

    public async Task SaveWeeklyBatch(IEnumerable<WeeklyWeather> weeklies)
    {
        var tasks = weeklies.Select(async w =>
        {
            await SaveWeekly(w);
        });
        
        await Task.WhenAll(tasks);
    }
}