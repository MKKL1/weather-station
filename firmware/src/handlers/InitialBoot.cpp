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

    // Get the WiFiConfigManager instance (loads config via constructor)
    WiFiConfigManager& wifiConfig = WiFiConfigManager::getInstance();

    // *** CHANGE: Read config values via WiFiConfigManager getters ***
    // Add getter methods to WiFiConfigManager to access parameter values
    // Example assuming getters exist:
    const char* currentMqttServer = wifiConfig.getMqttServer(); // Assuming getter exists
    // If no direct getter, and _paramMqttAddr is private, you might need to store it locally
    // OR modify WiFiConfigManager to expose it. For now, let's use a hypothetical getter.

    // *** Use the value obtained from WiFiConfigManager ***
    Serial.printf("Config MQTT addr (from WCM): %s\n", currentMqttServer ? currentMqttServer : "NULL");

    if (cfgBtn.shouldEnterConfig()) {
        if (wifiConfig.forceAP()) {
            Serial.println("Config portal finished.");
            // *** Important: After portal save, WCM internal params AND ConfigStore::cfg are updated.
            // You might want to re-read the value here if needed immediately.
            currentMqttServer = wifiConfig.getMqttServer();
            Serial.printf("Config MQTT addr post-portal (from WCM): %s\n", currentMqttServer ? currentMqttServer : "NULL");
        }
    } else {
        if (wifiConfig.connect()) {
            // WiFi.SSID() gives the *currently connected* SSID
            // wifiConfig.getWiFiSSID() could potentially return the *configured* SSID
            Serial.println("Connected to WiFi: " + WiFi.SSID()); // Use actual connected SSID

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
             // Handle connection failure - maybe retry or enter portal?
        }
    }

    // *** Use the value obtained from WiFiConfigManager ***
    Serial.printf("InitialBoot: Config (from WCM): %s\n", currentMqttServer ? currentMqttServer : "NULL");

    // Make sure MQTT client uses the correct value (e.g., currentMqttServer)
    // MqttClient.setServer(currentMqttServer, wifiConfig.getMqttPort()); // Example

    sensor.reset(TimeManager::getRTCEpochTime());
    SleepController::configure();
    esp_deep_sleep_start();
}
