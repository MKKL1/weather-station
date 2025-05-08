#ifndef CONFIG_H
#define CONFIG_H
#include <hal/gpio_types.h>

namespace Config {
    // --- Network ---
    constexpr const char* WIFI_SSID = "dd-wrt";         // <<< YOUR WIFI SSID
    constexpr const char* WIFI_PASSWORD = ""; // <<< YOUR WIFI PASSWORD
    constexpr const char* MQTT_SERVER_IP = "192.168.33.115"; // <<< YOUR MQTT BROKER IP/HOSTNAME
    constexpr uint16_t MQTT_PORT = 1883;
    constexpr const char* MQTT_CLIENT_ID_PREFIX = "WS-";
    constexpr const char* SENSOR_ID = "SOME_IDENTIFIER";
    constexpr const char* MQTT_TOPIC_DATA = "weather/data";
    constexpr const char* MQTT_TOPIC_STATUS = "weather/status";

    // --- Hardware & Calibration ---
    // IMPORTANT: Choose an RTC GPIO pin that supports EXT0/EXT1 wakeup!
    // Common choices: 0, 2, 4, 12-15, 25-27, 32-39 (check your specific ESP32 board)
    constexpr gpio_num_t RAIN_SENSOR_PIN = GPIO_NUM_36; // <<< YOUR RAIN SENSOR PIN
    // IMPORTANT: Set this to YOUR rain gauge's specification
    constexpr float MM_PER_TIP = 0.2794f; // Millimeters of rain per tip event (e.g., 0.2794 for 0.01 inch)

    // --- Timing & Sleep ---
    // How often to wake up via Timer for reporting (in milliseconds)
    constexpr uint32_t REPORT_INTERVAL_MS = 30 * 1000; // 30 minutes
    constexpr uint64_t REPORT_INTERVAL_US = (uint64_t)REPORT_INTERVAL_MS * 1000ULL;
    constexpr uint32_t WIFI_CONNECT_TIMEOUT_MS = 15000; // Max time to wait for WiFi
    constexpr uint32_t MQTT_CONNECT_TIMEOUT_MS = 5000;  // Max time to wait for MQTT connect
    constexpr uint32_t NTP_TIMEOUT_MS = 10000;          // Max time to wait for NTP sync

    // --- Time Sync (NTP) ---
    constexpr const char* NTP_SERVER_1 = "pool.ntp.org";
    constexpr const char* NTP_SERVER_2 = "time.nist.gov";
    // Timezone: Kraśnik, Poland (CET/CEST with automatic DST)
    // CET-1CEST,M3.5.0/2,M10.5.0/3 = UTC+1(CET), UTC+2(CEST),
    // DST starts last Sun Mar 2am, ends last Sun Oct 3am
    constexpr const char* TZ_INFO = "CET-1CEST,M3.5.0/2,M10.5.0/3";

    // --- Misc ---
    constexpr size_t JSON_DOC_SIZE = 256; // Adjust if needed for JSON payload
    constexpr size_t MQTT_MSG_BUFFER_SIZE = 256; // Ensure >= JSON size + overhead
    constexpr size_t FAILED_REPORT_QUEUE_SIZE = 10;
}
#endif //CONFIG_H
