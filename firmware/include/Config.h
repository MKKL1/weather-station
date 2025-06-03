#ifndef CONFIG_H
#define CONFIG_H
#include <controllers/ConfigTrigger.h>
#include <controllers/StatusLED.h>
#include <hal/gpio_types.h>

namespace Config {
    constexpr const char* MQTT_CLIENT_ID_PREFIX = "WS-";
    constexpr gpio_num_t RAIN_SENSOR_PIN = GPIO_NUM_36;
    constexpr uint32_t REPORT_INTERVAL_MS = 30 * 1000;
    constexpr uint64_t REPORT_INTERVAL_US = (uint64_t)REPORT_INTERVAL_MS * 1000ULL;
    constexpr uint32_t WIFI_CONNECT_TIMEOUT_MS = 15000;
    constexpr uint32_t MQTT_CONNECT_TIMEOUT_MS = 5000;
    constexpr uint32_t NTP_TIMEOUT_MS = 10000;

    constexpr const char* NTP_SERVER_1 = "pool.ntp.org";
    constexpr const char* NTP_SERVER_2 = "time.nist.gov";
    // Timezone: Kraśnik, Poland (CET/CEST with automatic DST)
    // CET-1CEST,M3.5.0/2,M10.5.0/3 = UTC+1(CET), UTC+2(CEST),
    // DST starts last Sun Mar 2am, ends last Sun Oct 3am
    constexpr const char* TZ_INFO = "CET-1CEST,M3.5.0/2,M10.5.0/3";

    constexpr size_t JSON_DOC_SIZE = 256;
    constexpr size_t MQTT_MSG_BUFFER_SIZE = 256;
    constexpr size_t FAILED_MQTT_QUEUE_SIZE = 10;
    constexpr size_t MAX_RAIN_EVENTS = 256;
    const ConfigTrigger CONFIG_BUTTON_PIN{GPIO_NUM_14};
    const StatusLED AP_LED_PIN{GPIO_NUM_32};
}
#endif //CONFIG_H
