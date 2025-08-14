using IoTEventWorker.Domain.Models;
using weatherstation.eventhandler.Entities;

namespace IoTEventWorker.Domain.Services;

public class CosmosDbModelMapper
{
    public object ToDocument(AggregateModel<LatestStatePayload> model)
    {
        return new
        {
            id = model.Id,
            deviceId = model.DeviceId,
            docType = model.DocType,
            payload = new
            {
                lastEventTs = model.Payload.LastEventTs.ToString("O"),
                lastRawId = model.Payload.LastRawId,
                temperature = model.Payload.Temperature,
                humidity = model.Payload.Humidity,
                pressure = model.Payload.Pressure,
                rain = model.Payload.Rain != null ? ToDocument(model.Payload.Rain) : null,
            },
            ttl = -1
        };
    }

    public object ToDocument(Histogram histogram)
    {
        return new
        {
            data = histogram.Tips.ToList(),
            slotCount = histogram.SlotCount,
            slotSecs = histogram.SlotSecs,
            startTime = histogram.StartTime
        };
    }
}