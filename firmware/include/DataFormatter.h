#ifndef DATAFORMATTER_H
#define DATAFORMATTER_H

#include "WeatherData.h"

class DataFormatter {
public:
    virtual ~DataFormatter() = default;
    virtual size_t formatData(const WeatherData& data, uint8_t* buffer, size_t bufferSize) const = 0;
};
#endif //DATAFORMATTER_H
