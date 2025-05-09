#ifndef APPLICATION_H
#define APPLICATION_H

#include "RainSensor.h"
#include "handlers/TipEvent.h"
#include "handlers/InitialBoot.h"
#include "handlers/Report.h"

class Application {
public:
    Application();
    void init() const;
    void handleWakeup();
    static void goToSleep() ;

private:
    RainSensor::Sensor _sensor;
    TipEvent           _tipHandler;
    InitialBoot        _bootHandler;
    Report             _reportHandler;
};

#endif // APPLICATION_H