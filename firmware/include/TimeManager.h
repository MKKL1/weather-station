#ifndef TIME_MANAGER_H
#define TIME_MANAGER_H

#include <ctime>
#include <sys/time.h>
#include <Arduino.h>
#include <WiFi.h>
#include "Config.h"

class TimeManager {
private:
    bool m_timeSynchronized = false;

public:
    static bool isTimeValid(const time_t t) {
        return t > 946684800;
    }

    TimeManager() {
        setenv("TZ", Config::TZ_INFO, 1);
        tzset();
    }

    // Attempts NTP sync, requires WiFi to be connected beforehand.
    bool syncTimeWithNTP(const uint32_t timeoutMs) {
        if (!WiFi.isConnected()) {
             Serial.println("TimeManager Error: Cannot sync NTP, WiFi not connected.");
             m_timeSynchronized = false;
             return false;
        }

        Serial.print("Configuring time via NTP from "); Serial.println(Config::NTP_SERVER_1);
        configTzTime(Config::TZ_INFO, Config::NTP_SERVER_1, Config::NTP_SERVER_2);

        Serial.print("Waiting for NTP time sync");
        time_t now = time(nullptr);
        unsigned long startSync = millis();

        while (!isTimeValid(now) && millis() - startSync < timeoutMs) {
            delay(500);
            Serial.print(".");
            now = time(nullptr);
        }
        Serial.println();

        if (isTimeValid(now)) {
            m_timeSynchronized = true;
            Serial.print("Time synchronized via NTP: ");
            printFormattedTime(now);
            return true;
        } else {
            m_timeSynchronized = false;
            Serial.println("NTP time sync failed!");
            return false;
        }
    }

    // Gets the current time from the ESP32's internal clock (RTC or NTP-synced).
    // This is the best available time at the moment it's called.
    [[nodiscard]] time_t getRTCEpochTime() const {
        return time(nullptr);
    }

    // Checks if NTP sync succeeded in *this specific wake cycle*.
    [[nodiscard]] bool isTimeSynchronizedThisCycle() const {
        return m_timeSynchronized;
    }

    // Checks if the current system time appears valid (useful after boot).
    [[nodiscard]] bool isSystemTimeValid() const {
        return isTimeValid(getRTCEpochTime());
    }

    // Prints time in a readable format
    void printFormattedTime(time_t t) const {
         if (!isTimeValid(t)) {
            Serial.println(" [Invalid Time]");
            return;
         }
        struct tm timeinfo{};
        localtime_r(&t, &timeinfo); // Use thread-safe version
        char buffer[64];
        strftime(buffer, sizeof(buffer), "%A, %B %d %Y %H:%M:%S %Z (%z)", &timeinfo);
        Serial.println(buffer);
    }

    // Explicitly sets the ESP32's internal RTC. Useful after NTP sync.
    void setInternalRTC(time_t epochTime) {
        if (!isTimeValid(epochTime)) {
            Serial.println("TimeManager Warning: Attempted to set invalid time to RTC.");
            return;
        }
        const timeval tv = { epochTime, 0 };
        settimeofday(&tv, nullptr); // Set system time
        Serial.print("Internal RTC set to: ");
        printFormattedTime(epochTime);
    }
};
#endif // TIME_MANAGER_H