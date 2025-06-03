#include "handlers/Report.h"

#include <AppConfig.h>

#include "builders/EntryBuilder.h"
#include "services/ReportSender.h"
#include "controllers/SleepController.h"
#include "TimeManager.h"
#include "InstanceIdGen.h"
#include <Arduino.h>
#include <WiFiConfigManager.h>


void Report::handle(RainSensor::Sensor& sensor) {
    Serial.println("Wakeup Source: Timer");

    EntryBuilder builder(sensor);
    WeatherEntry entry = builder.buildCurrentEntry();

    // Get WiFiConfigManager instance. Constructor calls loadConfig -> readFs.
    WiFiConfigManager& wifiConfig = WiFiConfigManager::getInstance();

    const char* server = wifiConfig.getMqttServer();
    const uint16_t port = wifiConfig.getMqttPort();
    const char* chipId = wifiConfig.getChipId();
    const char* topic = wifiConfig.getMqttTopic();
    const float mmPerTip = wifiConfig.getMmPerTip();
    const char* mqttUser = wifiConfig.getMqttUser();
    const char* mqttPass = wifiConfig.getMqttPass();

    Serial.printf("Report: Config Read (via WCM): Server=%s, Port=%u\n",
                  server ? server : "NULL", port);

    if (!server || strlen(server) == 0 || !chipId || strlen(chipId) == 0 || !topic || strlen(topic) == 0) {
        Serial.println("ERROR: Invalid config obtained from WiFiConfigManager on wake-up!");
        SleepController::configure();
        esp_deep_sleep_start();
        return;
    }

    // Connect to WiFi
    if (!wifiConfig.connect()) {
        Serial.println("WiFi connection failed during report.");
        SleepController::configure();
        esp_deep_sleep_start();
        return; // Exit if WiFi fails
    }

    Serial.println("WiFi connected for reporting.");

    const uint32_t i_id = InstanceIdGen::getInstanceId();

    Serial.printf("Instance id: %u \n", i_id);
    ReportSender sender{
        server,
        port,
        chipId,
        topic,
        mqttUser,
        mqttPass,
        mmPerTip,
        i_id
    };

    Serial.printf("ReportSender initialized with: Server=%s, Port=%u, Chip=%s, Topic=%s, MMpT=%.2f\n",
                  server, port, chipId, topic, mmPerTip);

    sender.send(entry);
    Serial.println("Report potentially sent, going back to sleep.");
    SleepController::configure();
    esp_deep_sleep_start();
}