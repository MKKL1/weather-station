#include "handlers/Report.h"

#include <AppConfig.h>

#include "builders/EntryBuilder.h"
#include "services/ReportSender.h"
#include "controllers/SleepController.h"
#include "TimeManager.h"
#include <Arduino.h>
#include <WiFiConfigManager.h>

void Report::handle(RainSensor::Sensor& sensor) {
    Serial.println("Wakeup Source: Timer");

    EntryBuilder builder(sensor);
    WeatherEntry entry = builder.buildCurrentEntry();
    WiFiConfigManager wifiConfig;
    if (!wifiConfig.begin()) {
        Serial.println("Config portal failed");
    }

    auto conf = ConfigStore::getConfig();

    ReportSender sender{
        conf.mqttServer.c_str(),
        conf.mqttPort,
        conf.chipId.c_str(),
        conf.mqttDataTopic.c_str(),
        conf.mmPerTip
    };
    sender.send(entry);

    SleepController::configure();
    esp_deep_sleep_start();
}
