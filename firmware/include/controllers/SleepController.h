#ifndef SLEEP_CONTROLLER_H
#define SLEEP_CONTROLLER_H

#include "Config.h"
#include "RainSensor.h"

/**
 * Encapsulates configuration of deep-sleep and wakeup sources.
 */
namespace SleepController {
    void configure();
};

#endif // SLEEP_CONTROLLER_H
