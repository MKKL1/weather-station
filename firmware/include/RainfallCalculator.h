#ifndef RAINFALL_CALCULATOR_H
#define RAINFALL_CALCULATOR_H

#include <stdint.h>

class RainfallCalculator {
private:
    float m_mmPerTip;

public:
    RainfallCalculator(float mmPerTip) : m_mmPerTip(mmPerTip) {}

    float calculateRainfall(uint32_t tipCount) const {
        return static_cast<float>(tipCount) * m_mmPerTip;
    }
};
#endif // RAINFALL_CALCULATOR_H