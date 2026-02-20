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

        PrecipitationReading? precipitationVo = null;
        
        if (payload.Precipitation != null)
        {
            float mmPerTip = payload.MmPerTip ?? 0.2f;
            var sparseMmData = payload.Precipitation.Data.ToDictionary(
                kvp => kvp.Key, 
                kvp => kvp.Value * mmPerTip
            );
            
            precipitationVo = PrecipitationReading.Create(
                sparseMmData,
                payload.Precipitation.SlotSeconds,
                DateTimeOffset.FromUnixTimeSeconds(payload.Precipitation.StartTimeEpoch).ToUniversalTime(),
                payload.Precipitation.SlotCount // Use explicit count
            );
        }

        return WeatherReading.Create(
            deviceId,
            timestamp,
            payload.Temperature ?? 0,
            payload.HumidityPpm ?? 0,
            payload.PressurePa ?? 0,
            precipitationVo);
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
                Precipitation = request.Payload.Precipitation == null ? null : new ValidatedTelemetryDto.ValidatedPrecipitationBins
                {
                    Data = request.Payload.Precipitation.Data!,
                    SlotSeconds = request.Payload.Precipitation.SlotSeconds!.Value,
                    StartTimeEpoch = request.Payload.Precipitation.StartTimeEpoch!.Value,
                    SlotCount = request.Payload.Precipitation.SlotCount!.Value,
                }
            }
        };
    }
}