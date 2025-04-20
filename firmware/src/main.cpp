#include <WiFi.h>
#include <PubSubClient.h>
#include <esp_sleep.h>
#include <sys/time.h>
#include <Config.h>
#include <JsonDataFormatter.h>
#include <RainSensor.h>
#include <TimeManager.h>
#include <WiFiManager.h>
#include <MqttClientWrapper.h>
#include <WeatherData.h>
#include <WeatherEntry.h>

//For debug
inline void printBitmask(const uint8_t *mask, size_t len) {
    for (size_t i = 0; i < len; i++) {
        if (mask[i] < 0x10) Serial.print('0');
        Serial.print(mask[i], HEX);
        Serial.print(' ');
    }
    Serial.println();
}

// Refactored sendRaport without goto
void sendRaport(TimeManager& timeManager, const RainSensor& rainSensor) {
    Serial.println("Wakeup Source: Timer");
    Serial.println("--- Starting Reporting Cycle ---");

    WiFiManager wifiManager(Config::WIFI_SSID, Config::WIFI_PASSWORD);
    MqttClientWrapper mqttClient(Config::MQTT_SERVER_IP, Config::MQTT_PORT,
                                 Config::MQTT_CLIENT_ID_PREFIX, Config::MQTT_TOPIC_STATUS);

    bool wifiConnected = false; // Flag to track WiFi connection status
    bool reportSent = false;    // Flag to track if the report was successfully sent

    // 1. Try connecting WiFi
    if (wifiManager.connect(Config::WIFI_CONNECT_TIMEOUT_MS)) {
        wifiConnected = true; // Mark WiFi as connected
        Serial.println("WiFi connected successfully.");

        // 2. Sync Time (Best Effort) - Only attempt if WiFi is connected
        if (timeManager.syncTimeWithNTP(Config::NTP_TIMEOUT_MS)) {
            time_t syncedTime = timeManager.getRTCEpochTime(); // Use the synced time from NTP if successful
            timeManager.setInternalRTC(syncedTime);
            Serial.print("Time synced. ");
        } else {
            Serial.println("NTP Time sync failed. Continuing with potentially unsynced RTC time.");
        }

        // --- Time Alignment Logic ---
        const time_t report_trigger_time = timeManager.getRTCEpochTime(); // Actual time report function started
        Serial.print("Report trigger time: ");
        timeManager.printFormattedTime(report_trigger_time);

        // Calculate the END timestamp for the WeatherEntry, aligned DOWN to the nearest interval boundary.
        // This defines the end of the last interval covered by this entry.
        const uint32_t aligned_window_end_ts = (report_trigger_time / WeatherEntry::INTERVAL_DURATION_S) * WeatherEntry::INTERVAL_DURATION_S;

        // Calculate the START timestamp of the full 32-minute period covered by this WeatherEntry.
        const uint32_t entry_coverage_start_ts = aligned_window_end_ts - WeatherEntry::ENTRY_DURATION;

        Serial.print("Aligned Window End (WeatherEntry.timestamp_s): ");
        timeManager.printFormattedTime(aligned_window_end_ts); // e.g., 13:40:00
        Serial.print("WeatherEntry Covers From: ");
        timeManager.printFormattedTime(entry_coverage_start_ts);  // e.g., 13:08:00


        // 3. Try connecting MQTT - Only attempt if WiFi is connected
        if (mqttClient.connect()) {
            Serial.println("MQTT connected successfully.");

            WeatherEntry weather_entry;
            weather_entry.clearBitmask(); // Clear the bitmask first
            weather_entry.timestamp_s = report_trigger_time;

            Serial.println("Processing events for the aligned window...");
            int events_processed = 0;
            int events_in_range = 0;

            // Iterate through *all* stored events. incrementTipCount will filter them.
            // Assuming RainSensor::events() returns a collection (like std::vector or std::deque)
            // Note: Ensure RainSensor::events() holds events for AT LEAST WeatherEntry::ENTRY_DURATION
            for (const auto e_time : RainSensor::events()) {
                 // incrementTipCount checks if e_time is within [entry_start_ts, aligned_end_ts)
                 if (weather_entry.incrementTipCount(e_time, report_trigger_time)) {
                    // Serial.printf("  -> Added event at timestamp %u to interval index %lu\n",
                    //               e_time, (e_time - entry_start_ts) / WeatherEntry::INTERVAL_DURATION_S);
                    events_in_range++;
                 }
                 events_processed++;
            }

            Serial.printf("Processed %d total events, %d were within the current entry's %u-second window [%u, %u).\n",
                          events_processed, events_in_range, WeatherEntry::ENTRY_DURATION, aligned_window_end_ts, entry_coverage_start_ts);

            Serial.print("Final entry bitmask: ");
            printBitmask(weather_entry.tip_bitmask, WeatherEntry::BITMASK_SIZE_BYTES);

            // Add other sensor data (temperature, humidity, pressure) if available
            // weather_entry.temperature = ...;
            // weather_entry.humidity = ...;
            // weather_entry.pressure = ...;


            const WeatherData weatherData(weather_entry, DeviceInfo(Config::SENSOR_ID, Config::MM_PER_TIP));
            const JsonDataFormatter formatter{};
            // Allocate on stack if possible, or ensure proper deletion if using new[]
            char messageBuffer[Config::MQTT_MSG_BUFFER_SIZE];
            formatter.formatData(weatherData, messageBuffer, Config::MQTT_MSG_BUFFER_SIZE);

            if (mqttClient.publish("weather/update", messageBuffer)) {
                 Serial.println("MQTT message published successfully.");
                 reportSent = true; // Mark report as sent

                 // !!! IMPORTANT !!!
                 // Add logic here to prune events from RainSensor's persistent storage
                 // that are now *older* than the start of the window we just reported.
                 // Only do this *after* successful transmission.
                 // Example: RainSensor::pruneEventsOlderThan(entry_start_ts);

            } else {
                 Serial.println("MQTT message publish failed.");
                 // Data remains in RainSensor buffer for next attempt
            }

            delay(100); // Allow time for MQTT operations
            mqttClient.disconnect();
            Serial.println("MQTT disconnected.");
            // --- End of MQTT Connected Block ---

        } else { // MQTT connection failed
            Serial.println("MQTT connection failed. Data remains in buffer.");
            // No need to publish MQTT status if the connection failed.
            // WiFi is still connected at this point and will be disconnected below.
        }

    } else { // WiFi connection failed
        Serial.println("WiFi connection failed. Skipping report cycle.");
        // Data remains in RainSensor buffer for next attempt
        // wifiConnected remains false
    }

    // --- Cleanup and Sleep Preparation ---
    // This section executes regardless of the success/failure of the above steps.

    // 9. Disconnect WiFi *if* it was connected
    if (wifiConnected) {
        wifiManager.disconnect();
        Serial.println("WiFi disconnected.");
    }

    Serial.println("--- Reporting Cycle Ended or Skipped ---");
    Serial.println("Configuring sleep (Timer + GPIO)...");
    rainSensor.setupWakeupPin(); // Setup GPIO wakeup
    esp_sleep_enable_timer_wakeup(Config::REPORT_INTERVAL_US); // Setup Timer wakeup
    Serial.println("Entering deep sleep...");
    delay(100); // Allow serial print to finish
    esp_deep_sleep_start();

    // The program should not reach here as esp_deep_sleep_start() does not return.
    Serial.println("!!! ERROR: Code continued after deep sleep call? !!!");
}

void syncTime(TimeManager& timeManager) {
    WiFiManager wifiManager(Config::WIFI_SSID, Config::WIFI_PASSWORD);
    if (wifiManager.connect(Config::WIFI_CONNECT_TIMEOUT_MS)) {
        if (timeManager.syncTimeWithNTP(Config::NTP_TIMEOUT_MS)) {
            const time_t syncedTime = timeManager.getRTCEpochTime();
            timeManager.setInternalRTC(syncedTime);
            Serial.print("Time synced. ");
        } else {
            Serial.println("NTP Time sync failed. Continuing with potentially unsynced time.");
        }
        wifiManager.disconnect();
    }
}

// The rest of your main.cpp (setup, loop) remains the same as the previous version
void setup() {
    delay(500);
    Serial.begin(115200);
    delay(100);

    Serial.println("\n\n--- ESP32 Rain Gauge Booting ---");
    Serial.print("Current Time (RTC): ");
    TimeManager timeManager;
    timeManager.printFormattedTime(timeManager.getRTCEpochTime());
    const RainSensor rainSensor(Config::RAIN_SENSOR_PIN);

    const esp_sleep_wakeup_cause_t wakeup_reason = esp_sleep_get_wakeup_cause();
    switch(wakeup_reason) {
        case ESP_SLEEP_WAKEUP_EXT0:
        {
            Serial.println("Wakeup Source: Rain Tip GPIO");
            const time_t now = timeManager.getRTCEpochTime();
            if (now > 1672531200) { // Check if time seems valid (e.g., after 1st Jan 2023)
                timeManager.printFormattedTime(now);
                RainSensor::handleTipWakeupEvent(now);
            } else {
                Serial.println("RTC time seems invalid, tip not logged.");
            }

            Serial.println("Configuring sleep (Timer + GPIO)...");
            rainSensor.setupWakeupPin();
            esp_sleep_enable_timer_wakeup(Config::REPORT_INTERVAL_US);
            Serial.println("Entering deep sleep...");
            delay(100);
            esp_deep_sleep_start();
            break;
        }
        case ESP_SLEEP_WAKEUP_TIMER:
        {
            sendRaport(timeManager, rainSensor);
            // sendRaport now handles going back to sleep internally
            break; // Should not be reached
        }
        default:
            Serial.printf("Wakeup Source: Other/Reset (%d)\n", wakeup_reason);
            Serial.println("Configuring sleep (Timer + GPIO)...");
            rainSensor.setupWakeupPin();
            syncTime(timeManager);
            RainSensor::resetPersistedData(timeManager.getRTCEpochTime());
            esp_sleep_enable_timer_wakeup(Config::REPORT_INTERVAL_US);
            Serial.println("Initial boot or Reset. Entering deep sleep...");
            delay(100);
            esp_deep_sleep_start();
            break;
    }
}


void loop() {
    Serial.println("!!! ERROR: Reached loop() - Deep sleep logic failed? !!!");
    delay(10000);
    esp_sleep_enable_timer_wakeup(Config::REPORT_INTERVAL_US); // Re-enable timer just in case
    esp_deep_sleep_start();
}