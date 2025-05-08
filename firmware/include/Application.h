#ifndef APPLICATION_H
#define APPLICATION_H

#include "Config.h"
#include "RainSensor.h"
#include "handlers/IHandler.h"
#include "handlers/TipEvent.h"
#include "handlers/InitialBoot.h"
#include "handlers/Report.h"
#include "TimeManager.h"

class Application {
public:
    Application();
    void init() const;
    void handleWakeup();
    void goToSleep() const;

private:
    RainSensor::Sensor _sensor;
    TipEvent           _tipHandler;
    InitialBoot        _bootHandler;
    Report             _reportHandler;
};

#endif // APPLICATION_H