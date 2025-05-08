#ifndef TIP_EVENT_H
#define TIP_EVENT_H

#include "IHandler.h"

class TipEvent : public IHandler {
public:
    void handle(RainSensor::Sensor& sensor) override;
};

#endif // TIP_EVENT_H