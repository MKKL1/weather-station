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

    public async Task SaveRawTelemetry(ValidatedTelemetryDto telemetryRequest, string deviceId)
    {
        var document = mapper.ToRawDocument(telemetryRequest, deviceId);
        
        await _rawContainer.UpsertItemAsync(
            document, 
            new PartitionKey(deviceId)
        );
    }

    public async Task<List<DailyWeather>> GetDailyBatch(string readingDeviceId, List<DateTimeOffset> affectedDates)
    {
        if (affectedDates.Count == 0) return [];

        var ids = affectedDates
            .Select(date => (
                id: IdBuilder.BuildDaily(readingDeviceId, date), 
                pk: new PartitionKey(readingDeviceId))
            )
            .ToList();

        var response = await _viewsContainer.ReadManyItemsAsync<DailyWeatherDocument>(
            ids.Select(x => (x.id, x.pk)).ToList()
        );
        
        return response.Resource.Select(mapper.ToDomain).ToList();
    }

    public async Task SaveStateUpdate(WeatherStateUpdate weatherStateUpdate)
    {
        var deviceId = weatherStateUpdate.CurrentReading.DeviceId;
        var batch = _viewsContainer.CreateTransactionalBatch(new PartitionKey(deviceId));

        var currentDoc = mapper.ToDocument(weatherStateUpdate.CurrentReading);
        batch.UpsertItem(currentDoc);
        
        foreach (var daily in weatherStateUpdate.DailyChanges)
        {
            var doc = mapper.ToDocument(daily);
            batch.UpsertItem(doc);
        }

        using var response = await batch.ExecuteAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Failed to save weather batch for {deviceId}: {response.ErrorMessage}");
        }
    }
}