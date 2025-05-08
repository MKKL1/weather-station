#ifndef TIME_SYNC_SERVICE_H
#define TIME_SYNC_SERVICE_H

#include "WiFiManager.h"
#include "TimeManager.h"

/**
 * Service to synchronize the ESP32 RTC via NTP at boot/reset.
 */
namespace TimeSyncService {
    void sync();
};

#endif // TIME_SYNC_SERVICE_H
