#include "Application.h"
#include "controllers/SleepController.h"
#include <esp_sleep.h>
#include <Arduino.h>
#include <controllers/SleepController.h>

Application::Application()
  : _sensor(Config::RAIN_SENSOR_PIN),
    _tipHandler(),
    _bootHandler(),
    _reportHandler()
{}

void Application::init() const {
    Serial.begin(115200);
    delay(100);
    Serial.println("\n\n--- ESP32 Weather Station Booting ---");
    TimeManager::initTimezone();
    _sensor.begin();
}

void Application::handleWakeup() {
    const auto cause = esp_sleep_get_wakeup_cause();
    IHandler* handler = nullptr;
    switch (cause) {
        case ESP_SLEEP_WAKEUP_EXT0:
            handler = &_tipHandler;
        break;
        case ESP_SLEEP_WAKEUP_TIMER:
            handler = &_reportHandler;
        break;
        default:
            handler = &_bootHandler;
        break;
    }
    handler->handle(_sensor);
}

void Application::goToSleep() const {
    SleepController::configure();
    esp_deep_sleep_start();
}