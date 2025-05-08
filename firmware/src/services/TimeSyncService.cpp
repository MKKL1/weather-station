#include "services/TimeSyncService.h"
#include <Arduino.h>

void TimeSyncService::sync() {
    if (!WiFiManager::connect(Config::WIFI_SSID, Config::WIFI_PASSWORD, Config::WIFI_CONNECT_TIMEOUT_MS)) {
        Serial.println("WiFi for time sync failed");
        return;
    }
    Serial.println("WiFi connected for time sync");

    if (TimeManager::syncTimeWithNTP(Config::NTP_TIMEOUT_MS)) {
        time_t now = TimeManager::getRTCEpochTime();
        TimeManager::setInternalRTC(now);
        Serial.print("Time synced: ");
        TimeManager::printFormattedTime(now);
    } else {
        Serial.println("NTP time sync failed");
    }
    WiFiManager::disconnect();
}