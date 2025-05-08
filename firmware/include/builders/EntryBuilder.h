#ifndef ENTRY_BUILDER_H
#define ENTRY_BUILDER_H

#include "WeatherEntry.h"
#include "RainSensor.h"

/**
 * Builds a WeatherEntry covering the interval ending at the last
 * aligned timestamp, populating rain tip bitmask and other fields.
 */
class EntryBuilder {
public:
    explicit EntryBuilder(RainSensor::Sensor& sensor);
    WeatherEntry buildCurrentEntry();

private:
    RainSensor::Sensor& _sensor;
};

#endif // ENTRY_BUILDER_H
