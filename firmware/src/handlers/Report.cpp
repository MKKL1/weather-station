#include "handlers/Report.h"
#include "builders/EntryBuilder.h"
#include "services/ReportSender.h"
#include "controllers/SleepController.h"
#include "TimeManager.h"
#include <Arduino.h>

void Report::handle(RainSensor::Sensor& sensor) {
    Serial.println("Wakeup Source: Timer");

    EntryBuilder builder(sensor);
    WeatherEntry entry = builder.buildCurrentEntry();

    ReportSender sender{
        Config::MQTT_SERVER_IP,
        Config::MQTT_PORT,
        Config::MQTT_CLIENT_ID_PREFIX
    };
    sender.send(entry);

    SleepController::configure();
    esp_deep_sleep_start();
}