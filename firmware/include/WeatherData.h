#ifndef WEATHERDATA_H
#define WEATHERDATA_H

#include <WeatherEntry.h>
#include <string_view>

struct DeviceInfo {
    std::string      sensorId;
    float            mmPerTip{};
    uint32_t         instanceId;

    explicit DeviceInfo(const std::string_view id, const float mm, const uint32_t instanceId) noexcept
        : sensorId(id), mmPerTip(mm), instanceId(instanceId) {
    }
};

struct WeatherData {
    WeatherEntry weatherEntry;
    DeviceInfo   deviceInfo;

    WeatherData(const WeatherEntry &weatherEntry, const DeviceInfo &deviceInfo): weatherEntry(weatherEntry), deviceInfo(deviceInfo) {}
};

#endif //WEATHERDATA_H
