#ifndef REPORT_H
#define REPORT_H

#include "IHandler.h"

class Report : public IHandler {
public:
    void handle(RainSensor::Sensor& sensor) override;
};

#endif // REPORT_H