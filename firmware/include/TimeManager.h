// time_manager.h
#ifndef TIME_MANAGER_H
#define TIME_MANAGER_H

#include <ctime>
#include <sys/time.h>
#include <Arduino.h>
#include "Config.h"

namespace TimeManager {

    // Checks whether the given epoch time is after Jan 1, 2000 UTC
    inline bool isTimeValid(time_t t) {
        return t > 946684800;
    }

    // Initializes the timezone from Config (call once in setup)
    inline void initTimezone() {
        setenv("TZ", Config::TZ_INFO, 1);
        tzset();
    }

    // Attempts to synchronize time via NTP; returns true on success
    bool syncTimeWithNTP(uint32_t timeoutMs);

    // Returns the current system time (RTC or NTP)
    inline time_t getRTCEpochTime() {
        return time(nullptr);
    }

    // Prints formatted time to Serial
    void printFormattedTime(time_t t);

    // Sets the ESP32's RTC to the given epoch time
    void setInternalRTC(time_t epochTime);

} // namespace TimeManager

#endif // TIME_MANAGER_H