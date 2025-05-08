// time_manager.cpp
#include "TimeManager.h"
#include <Arduino.h>
#include <WiFi.h>
#include "Config.h"

// #define TIME_MANAGER_DEBUG
#ifdef TIME_MANAGER_DEBUG
  #define TM_LOG(...)    Serial.printf(__VA_ARGS__)
  #define TM_LOGLN(...)  Serial.println(__VA_ARGS__)
#else
  #define TM_LOG(...)
  #define TM_LOGLN(...)
#endif

namespace TimeManager {

    bool syncTimeWithNTP(uint32_t timeoutMs) {
        if (!WiFi.isConnected()) {
            TM_LOGLN("TimeManager Error: Cannot sync NTP, WiFi not connected.");
            return false;
        }

        TM_LOG("Configuring time via NTP from %s", Config::NTP_SERVER_1);
        configTzTime(Config::TZ_INFO, Config::NTP_SERVER_1, Config::NTP_SERVER_2);

        TM_LOG("Waiting for NTP time sync");
        time_t now = time(nullptr);
        unsigned long start = millis();

        while (!isTimeValid(now) && millis() - start < timeoutMs) {
            delay(500);
            TM_LOG(".");
            now = time(nullptr);
        }
        TM_LOGLN("");

        if (isTimeValid(now)) {
            TM_LOG("Time synchronized via NTP: ");
            printFormattedTime(now);
            return true;
        }

        TM_LOGLN("NTP time sync failed!");
        return false;
    }

    void printFormattedTime(time_t t) {
        if (!isTimeValid(t)) {
            TM_LOGLN("[Invalid Time]");
            return;
        }

        struct tm timeinfo;
        localtime_r(&t, &timeinfo);
        char buf[64];
        strftime(buf, sizeof(buf), "%A, %B %d %Y %H:%M:%S %Z (%z)", &timeinfo);
        TM_LOGLN(buf);
    }

    void setInternalRTC(time_t epochTime) {
        if (!isTimeValid(epochTime)) {
            TM_LOGLN("TimeManager Warning: Attempted to set invalid time to RTC.");
            return;
        }

        timeval tv = { epochTime, 0 };
        settimeofday(&tv, nullptr);
        TM_LOG("Internal RTC set to: ");
        printFormattedTime(epochTime);
    }

}
