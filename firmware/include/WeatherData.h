#ifndef WEATHERDATA_H
#define WEATHERDATA_H

#include <WeatherEntry.h>
#include <string_view>

struct DeviceInfo {
    std::string_view sensorId;
    float            mmPerTip{};

    explicit constexpr DeviceInfo(const std::string_view id, const float mm = 0.0f) noexcept
      : sensorId(id), mmPerTip(mm)
    {}
};

struct WeatherData {
    WeatherEntry weatherEntry;
    DeviceInfo   deviceInfo;

    WeatherData(const WeatherEntry &weatherEntry, const DeviceInfo &deviceInfo): weatherEntry(weatherEntry), deviceInfo(deviceInfo) {}
};

#endif //WEATHERDATA_H
