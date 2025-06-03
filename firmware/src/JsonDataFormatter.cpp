#include "JsonDataFormatter.h"
#include <base64.h>

inline void printBitmask(const uint8_t *mask, size_t len) {
    for (size_t i = 0; i < len; i++) {
        if (mask[i] < 0x10) Serial.print('0');
        Serial.print(mask[i], HEX);
        Serial.print(' ');
    }
    Serial.println();
}


size_t JsonDataFormatter::formatData(const WeatherData &data, uint8_t *buffer, size_t bufferSize) const {
    if (!buffer || bufferSize == 0) return 0;

    JsonDocument jsonDoc;
    jsonDoc["time"] = data.weatherEntry.timestamp_s;
    jsonDoc["temp"] = data.weatherEntry.temperature;
    jsonDoc["humidity"] = data.weatherEntry.humidity;
    jsonDoc["pressure"] = data.weatherEntry.pressure;

    Serial.print("Bitmask: ");
    printBitmask(data.weatherEntry.tip_bitmask, WeatherEntry::BITMASK_SIZE_BYTES);
    Serial.print("Formatted to base64: ");
    Serial.println(base64::encode(data.weatherEntry.tip_bitmask, WeatherEntry::BITMASK_SIZE_BYTES));
    jsonDoc["tip_events"] = base64::encode(data.weatherEntry.tip_bitmask, WeatherEntry::BITMASK_SIZE_BYTES);

    const auto deviceInfo = jsonDoc["device_info"];
    deviceInfo["sensor_id"] = data.deviceInfo.sensorId;
    deviceInfo["mm_per_tip"] = data.deviceInfo.mmPerTip;

    const size_t bytesWritten = serializeJson(jsonDoc, buffer, bufferSize);

    if (bytesWritten == 0 || bytesWritten >= bufferSize) {
        Serial.println("JSON serialization failed - buffer too small or other error");
        // if (bufferSize > 30) {
        //     snprintf(buffer, bufferSize, "{\"error\":\"json buffer overflow\"}");
        //     return strlen(buffer);
        // }
        return 0;
    }

    return bytesWritten;
}

