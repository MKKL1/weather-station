using Worker.Domain.Entities;
using Worker.Domain.ValueObjects;
using Worker.Dto;
using Worker.Infrastructure.Documents;

namespace Worker.Mappers;

/// <summary>
/// Maps telemetry DTOs to domain entities.
/// Validation and sanitization happen in the domain layer through value objects.
/// </summary>
public class TelemetryMapper
{
    public WeatherReading ToDomain(TelemetryRequest dto, string deviceId)
    {
        var timestamp = DateTimeOffset.FromUnixTimeSeconds(dto.TimestampEpoch);
        var payload = dto.Payload;

        Rainfall? rainVo = null;

        // check if we have rain data
        if (dto.Payload.Rain != null && dto.Payload.Rain.Data.Length > 0)
        {
            float mmPerTip = dto.Payload.MmPerTip ?? 0.2f; 
            var mmValues = dto.Payload.Rain.Data
                .Select(tips => tips * mmPerTip)
                .ToArray();
            rainVo = Rainfall.Create(
                mmValues,
                dto.Payload.Rain.SlotSeconds,
                DateTimeOffset.FromUnixTimeSeconds(dto.Payload.Rain.StartTimeEpoch)
            );
        }

        return WeatherReading.Create(
            deviceId,
            timestamp,
            payload.Temperature ?? 0,
            payload.HumidityPpm ?? 0,
            payload.PressurePa ?? 0,
            rainVo);
    }
}