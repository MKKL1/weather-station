//
// Created by krystian on 23.04.2025.
//

#ifndef PROTODATAFORMATTER_H
#define PROTODATAFORMATTER_H

#include "DataFormatter.h"
#include "WeatherData.h"

class ProtoDataFormatter final : public DataFormatter {
public:
    size_t formatData(const WeatherData& data, uint8_t* buffer, size_t bufferSize) const override;
};



#endif //PROTODATAFORMATTER_H
