#include "handlers/TipEvent.h"
#include "controllers/SleepController.h"
#include "TimeManager.h"
#include <Arduino.h>
#include <esp_sleep.h>

void TipEvent::handle(RainSensor::Sensor& sensor) {
    Serial.println("Wakeup Source: Rain Tip GPIO");
    time_t now = TimeManager::getRTCEpochTime();
    if (TimeManager::isTimeValid(now)) {
        Serial.print("Logging tip at: ");
        TimeManager::printFormattedTime(now);
        sensor.handleTip(now);
    } else {
        Serial.println("RTC time invalid, tip not logged.");
    }
    SleepController::configure();
    esp_deep_sleep_start();
}