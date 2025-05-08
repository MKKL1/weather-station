#include "handlers/InitialBoot.h"
#include "WiFiManager.h"
#include "controllers/SleepController.h"
#include "TimeManager.h"
#include <Arduino.h>
#include <esp_sleep.h>

void InitialBoot::handle(RainSensor::Sensor& sensor) {
    Serial.printf("Wakeup Source: Other/Reset (%d)\n", esp_sleep_get_wakeup_cause());
    Serial.println("Initial setup...");

    if (WiFiManager::connect(Config::WIFI_SSID, Config::WIFI_PASSWORD, Config::WIFI_CONNECT_TIMEOUT_MS)) {
        if (TimeManager::syncTimeWithNTP(Config::NTP_TIMEOUT_MS)) {
            time_t synced = TimeManager::getRTCEpochTime();
            TimeManager::setInternalRTC(synced);
            Serial.print("Time synced: ");
            TimeManager::printFormattedTime(synced);
        } else {
            Serial.println("NTP sync failed.");
        }
        WiFiManager::disconnect();
    }

    sensor.reset(TimeManager::getRTCEpochTime());
    SleepController::configure();
    esp_deep_sleep_start();
}