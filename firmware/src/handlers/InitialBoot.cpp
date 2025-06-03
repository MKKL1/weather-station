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

    WiFiConfigManager& wifiConfig = WiFiConfigManager::getInstance();
    const char* currentMqttServer = wifiConfig.getMqttServer();
    Serial.printf("Config MQTT addr (from WCM): %s\n", currentMqttServer ? currentMqttServer : "NULL");

    if (cfgBtn.shouldEnterConfig()) {
        if (wifiConfig.forceAP()) {
            Serial.println("Config portal finished.");
            currentMqttServer = wifiConfig.getMqttServer();
            Serial.printf("Config MQTT addr post-portal (from WCM): %s\n", currentMqttServer ? currentMqttServer : "NULL");
        }
    } else {
        if (wifiConfig.connect()) {
            Serial.println("Connected to WiFi: " + WiFi.SSID());

            if (TimeManager::syncTimeWithNTP(Config::NTP_TIMEOUT_MS)) {
                time_t synced = TimeManager::getRTCEpochTime();
                TimeManager::setInternalRTC(synced);
                Serial.print("Time synced: ");
                TimeManager::printFormattedTime(synced);
            } else {
                Serial.println("NTP sync failed.");
            }
        } else {
             Serial.println("WiFi Connection Failed.");
        }
    }

    Serial.printf("InitialBoot: Config (from WCM): %s\n", currentMqttServer ? currentMqttServer : "NULL");

    sensor.reset(TimeManager::getRTCEpochTime());
    SleepController::configure();
    esp_deep_sleep_start();
}
