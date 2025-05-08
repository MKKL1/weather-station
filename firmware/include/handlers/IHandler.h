#ifndef IHANDLER_H
#define IHANDLER_H

#include "RainSensor.h"

class IHandler {
public:
    virtual ~IHandler() = default;
    virtual void handle(RainSensor::Sensor& sensor) = 0;
};

#endif // IHANDLER_H