#include "builders/EntryBuilder.h"
#include <Arduino.h>
#include <TimeManager.h>

EntryBuilder::EntryBuilder(RainSensor::Sensor& sensor)
    : _sensor(sensor) {}

WeatherEntry EntryBuilder::buildCurrentEntry() {
    // 1) Get RTC now and align to interval boundary
    time_t now = TimeManager::getRTCEpochTime();
    uint32_t alignedEnd = (now / WeatherEntry::INTERVAL_DURATION_S) * WeatherEntry::INTERVAL_DURATION_S;
    uint32_t start     = alignedEnd - WeatherEntry::ENTRY_DURATION;

    Serial.print("Building entry window [");
    TimeManager::printFormattedTime(start);
    Serial.print(" to ");
    TimeManager::printFormattedTime(alignedEnd);
    Serial.println("]");

    // 2) Create entry and clear bitmask
    WeatherEntry entry;
    entry.clearBitmask();
    entry.timestamp_s = alignedEnd;
    entry.humidity = 20;
    entry.temperature = 30;
    entry.pressure = 100;

    // 3) Populate rain-tip bitmask
    int processed = 0, inRange = 0;
    for (time_t t : RainSensor::Sensor::events()) {
        processed++;
        if (entry.incrementTipCount(t, alignedEnd)) {
            inRange++;
        }
    }
    Serial.printf("Processed %d events, %d in window.\n", processed, inRange);

    return entry;
}
