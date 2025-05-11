#include "handlers/InitialBoot.h"
#include "controllers/SleepController.h"
#include "controllers/ConfigTrigger.h"
#include "TimeManager.h"
#include <Arduino.h>
#include <esp_sleep.h>
#include <WiFiConfigManager.h>

void InitialBoot::handle(RainSensor::Sensor& sensor) {
    Serial.printf("Wakeup Source: Other/Reset (%d)\n", esp_sleep_get_wakeup_cause());
    Serial.println("Initial setup...");

    const ConfigTrigger cfgBtn(Config::CONFIG_BUTTON_PIN);
    WiFiConfigManager wifiConfig{};

    if (cfgBtn.shouldEnterConfig()) {
        // force AP mode
        if (wifiConfig.forceAP()) {
            Serial.println("Config portal finished.");
        }
    }
    else {
        // normal WiFi connect + NTP sync
        if (wifiConfig.begin()) {
            Serial.println("Connected to WiFi: " + ConfigStore::getConfig().wifiSSID);

            if (TimeManager::syncTimeWithNTP(Config::NTP_TIMEOUT_MS)) {
                time_t synced = TimeManager::getRTCEpochTime();
                TimeManager::setInternalRTC(synced);
                Serial.print("Time synced: ");
                TimeManager::printFormattedTime(synced);
            } else {
                Serial.println("NTP sync failed.");
            }
        }
    }

    sensor.reset(TimeManager::getRTCEpochTime());
    SleepController::configure();
    esp_deep_sleep_start();
}
