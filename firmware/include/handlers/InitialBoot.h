#ifndef INITIAL_BOOT_H
#define INITIAL_BOOT_H

#include "IHandler.h"

class InitialBoot : public IHandler {
public:
    void handle(RainSensor::Sensor& sensor) override;
};

#endif // INITIAL_BOOT_H