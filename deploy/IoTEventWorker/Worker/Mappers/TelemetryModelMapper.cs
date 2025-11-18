using Worker.Infrastructure.Documents;

namespace Worker.Mappers;

public class TelemetryModelMapper : ITelemetryModelMapper
{
    public RawEventDocument ToDocument(TelemetryDocument telemetry, string deviceId, string eventType)
    {
        if (telemetry is null) throw new ArgumentNullException(nameof(telemetry));
        if (telemetry.Payload is null) throw new ArgumentException("Payload cannot be null", nameof(telemetry));
        if (string.IsNullOrWhiteSpace(deviceId)) throw new ArgumentException("deviceId cannot be null/empty", nameof(deviceId));

        var eventTimestamp = DateTimeOffset.FromUnixTimeMilliseconds(telemetry.TimestampEpochMs);

        // Histogram defaults
        string dataBase64 = string.Empty;
        byte slotCount = 0;
        ushort slotSecs = 0;
        DateTimeOffset startTime = DateTimeOffset.UnixEpoch;

        if (telemetry.Payload.Rain is not null)
        {
            if (!string.IsNullOrWhiteSpace(telemetry.Payload.Rain.DataBase64))
            {
                dataBase64 = NormalizeBase64ToStandard(telemetry.Payload.Rain.DataBase64);
            }

            slotCount = ClampToByte(telemetry.Payload.Rain.SlotCount);
            slotSecs = ClampToUShort(telemetry.Payload.Rain.SlotSeconds);

            if (telemetry.Payload.Rain.StartTimeEpochMs > 0)
            {
                startTime = DateTimeOffset.FromUnixTimeMilliseconds(telemetry.Payload.Rain.StartTimeEpochMs);
            }
        }

        var payload = new RawEventDocument.PayloadBody
        {
            Temperature = telemetry.Payload.Temperature.GetValueOrDefault(),
            Humidity = telemetry.Payload.HumidityPpm.GetValueOrDefault(),
            Pressure = telemetry.Payload.PressurePa.GetValueOrDefault(),
            Rain = new RawEventDocument.Histogram
            {
                Data = dataBase64,
                SlotCount = slotCount,
                SlotSecs = slotSecs,
                StartTime = startTime
            },
            RainfallMMPerTip = telemetry.Payload.MmPerTip.GetValueOrDefault()
        };

        return new RawEventDocument
        {
            id = $"{deviceId}|{eventTimestamp:yyyy-MM-ddTHH:mm:ss}",
            DeviceId = deviceId,
            EventType = eventType,
            EventTimestamp = eventTimestamp,
            Payload = payload
        };
    }

    private static string NormalizeBase64ToStandard(string maybeBase64OrBase64Url)
    {
        if (string.IsNullOrWhiteSpace(maybeBase64OrBase64Url)) return string.Empty;

        var s = maybeBase64OrBase64Url.Trim()
            .Replace('-', '+')
            .Replace('_', '/');

        // pad
        var pad = s.Length % 4;
        if (pad > 0) s = s + new string('=', 4 - pad);

        // validate by decoding/re-encoding to canonical base64
        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(s);
        }
        catch (FormatException ex)
        {
            throw new ArgumentException("Histogram data is not valid base64/base64url.", ex);
        }

        return Convert.ToBase64String(bytes);
    }

    private static byte ClampToByte(int value)
    {
        if (value <= byte.MinValue) return byte.MinValue;
        if (value >= byte.MaxValue) return byte.MaxValue;
        return (byte)value;
    }

    private static ushort ClampToUShort(int value)
    {
        if (value <= ushort.MinValue) return ushort.MinValue;
        if (value >= ushort.MaxValue) return ushort.MaxValue;
        return (ushort)value;
    }
}