#ifndef JSON_DATA_FORMATTER_H
#define JSON_DATA_FORMATTER_H

#include <ArduinoJson.h>
#include "DataFormatter.h"
#include "WeatherData.h"

class JsonDataFormatter final : public DataFormatter {
public:
    size_t formatData(const WeatherData& data, uint8_t* buffer, size_t bufferSize) const override;
};
#endif // JSON_DATA_FORMATTER_H