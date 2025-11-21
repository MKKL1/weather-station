using Worker.Domain.Entities;
using Worker.Domain.ValueObjects;
using Worker.Dto;

namespace Worker.Mappers;

public class TelemetryMapper
{
    public WeatherReading ToDomain(ValidatedTelemetryDto dto, string deviceId)
    {
        var timestamp = DateTimeOffset.FromUnixTimeSeconds(dto.TimestampEpoch).ToUniversalTime();
        var payload = dto.Payload;

        RainfallReading? rainVo = null;
        
        if (payload.Rain != null)
        {
            float mmPerTip = payload.MmPerTip ?? 0.2f; 
            var mmValues = payload.Rain.Data
                .Select(tips => tips * mmPerTip)
                .ToArray();
            
            rainVo = RainfallReading.Create(
                mmValues,
                payload.Rain.SlotSeconds,
                DateTimeOffset.FromUnixTimeSeconds(payload.Rain.StartTimeEpoch).ToUniversalTime()
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
    
    public ValidatedTelemetryDto ToValidatedDto(TelemetryRequest request)
    {
        return new ValidatedTelemetryDto
        {
            TimestampEpoch = request.TimestampEpoch!.Value,
            Payload = new ValidatedTelemetryDto.ValidatedPayload
            {
                Temperature = request.Payload!.Temperature,
                PressurePa = request.Payload.PressurePa,
                HumidityPpm = request.Payload.HumidityPpm,
                MmPerTip = request.Payload.MmPerTip,
                Rain = request.Payload.Rain == null ? null : new ValidatedTelemetryDto.ValidatedHistogram
                {
                    Data = request.Payload.Rain.Data!,
                    SlotSeconds = request.Payload.Rain.SlotSeconds!.Value,
                    StartTimeEpoch = request.Payload.Rain.StartTimeEpoch!.Value
                }
            }
        };
    }
}