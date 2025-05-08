#include "controllers/SleepController.h"
#include <esp_sleep.h>
#include <Arduino.h>

void SleepController::configure() {
    // RainSensor static helper to wire up GPIO wake pin
    //RainSensor::Sensor::begin();
    // Timer wakeup
    esp_sleep_enable_timer_wakeup(Config::REPORT_INTERVAL_US);
    Serial.println("Configured sleep (Timer + GPIO)");
}
