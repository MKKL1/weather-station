#include <WiFi.h>
#include <PubSubClient.h>
#include <esp_sleep.h>
#include <sys/time.h>
#include <cstring> // Required for memset in WeatherEntry if not already included

// --- Project Includes ---
#include <ProtoDataFormatter.h>

#include "Config.h" // Assuming FAILED_ENTRY_QUEUE_SIZE and MQTT_TOPIC_WEATHER are here
#include "JsonDataFormatter.h"
#include "RainSensor.h"
#include "TimeManager.h"
#include "WiFiManager.h"
#include "MqttClientWrapper.h"
#include "WeatherData.h"
#include "WeatherEntry.h"
#include "CircularQueue.h" // Include your CircularQueue implementation

// --- Constants ---
#ifndef FAILED_ENTRY_QUEUE_SIZE
#define FAILED_ENTRY_QUEUE_SIZE 10 // Example size, define in Config.h ideally
#endif

// --- Persistent Data ---
static RTC_DATA_ATTR CircularQueue<WeatherEntry, FAILED_ENTRY_QUEUE_SIZE> failedEntriesQueue;

// --- Helper Functions (printBitmask, attemptPublish, calculateEntryCoverageStartTs - unchanged from previous version) ---

inline void printBitmask(const uint8_t *mask, size_t len) {
    for (size_t i = 0; i < len; i++) {
        if (mask[i] < 0x10) Serial.print('0');
        Serial.print(mask[i], HEX);
    }
    Serial.println();
}

bool attemptPublish(MqttClientWrapper& mqttClient, const WeatherEntry& entry, const DataFormatter& formatter, const TimeManager& timeManager) {
    const WeatherData weatherData(entry, DeviceInfo(Config::SENSOR_ID, Config::MM_PER_TIP));
    uint8_t messageBuffer[Config::MQTT_MSG_BUFFER_SIZE];
    const auto size = formatter.formatData(weatherData, messageBuffer, Config::MQTT_MSG_BUFFER_SIZE);
    if (!size) {
        Serial.println("!!! ERROR: Failed to format WeatherData into JSON buffer (too small?)");
        return false;
    }
    Serial.print("Attempting to publish entry aligned to end timestamp: ");
    timeManager.printFormattedTime(entry.timestamp_s);

    Serial.print("Data: ");
    printBitmask(messageBuffer, size);
    Serial.println();

    Serial.print("Data entire: ");
    printBitmask(messageBuffer, Config::MQTT_MSG_BUFFER_SIZE);

    if (mqttClient.publish(Config::MQTT_TOPIC_DATA, messageBuffer, size)) {
        Serial.println("  -> Published successfully.");
        return true;
    } else {
        Serial.println("  -> Publish failed.");
        return false;
    }
}

uint32_t calculateEntryCoverageStartTs(const WeatherEntry& entry) {
    const uint32_t aligned_window_end_ts = (entry.timestamp_s / WeatherEntry::INTERVAL_DURATION_S) * WeatherEntry::INTERVAL_DURATION_S;
    return aligned_window_end_ts - WeatherEntry::ENTRY_DURATION;
}


// --- Core Logic ---

// Refactored sendRaport - Ensures current entry is ALWAYS created and queued on failure
void sendRaport(TimeManager& timeManager, const RainSensor& rainSensor) {
    Serial.println("Wakeup Source: Timer");
    Serial.println("--- Starting Reporting Cycle ---");

    // --- 1. Create and Populate Current Entry (ALWAYS) ---
    Serial.println("Processing current weather data...");
    const time_t report_trigger_time = timeManager.getRTCEpochTime();
    Serial.print("Report trigger time: ");
    timeManager.printFormattedTime(report_trigger_time);

    // Align the END timestamp DOWN to the nearest interval boundary.
    const uint32_t aligned_window_end_ts = (report_trigger_time / WeatherEntry::INTERVAL_DURATION_S) * WeatherEntry::INTERVAL_DURATION_S;
    // Calculate the START timestamp of the full period covered by this entry.
    const uint32_t entry_coverage_start_ts = aligned_window_end_ts - WeatherEntry::ENTRY_DURATION;

    Serial.print("Aligned Window End (Entry Timestamp): ");
    timeManager.printFormattedTime(aligned_window_end_ts);
    Serial.print("Current Entry Covers From: ");
    timeManager.printFormattedTime(entry_coverage_start_ts);

    WeatherEntry current_weather_entry; // Create the entry object
    current_weather_entry.clearBitmask();
    current_weather_entry.timestamp_s = report_trigger_time;

    // Add other sensor data (temperature, humidity, pressure) if available
    // current_weather_entry.temperature = readTemperature(); // Example

    // Populate bitmask from RainSensor events within the calculated window
    int events_processed = 0;
    int events_in_range = 0;
    for (const auto e_time : RainSensor::events()) {
        // Use the entry's ALIGNED end time for correct window calculation in incrementTipCount
        if (current_weather_entry.incrementTipCount(e_time, aligned_window_end_ts)) {
             events_in_range++;
        }
        events_processed++;
    }
    Serial.printf("Populated current entry: Processed %d total rain events, %d were within the window [%u, %u).\n",
                  events_processed, events_in_range, entry_coverage_start_ts, aligned_window_end_ts);
    Serial.print("Current entry final bitmask: ");
    printBitmask(current_weather_entry.tip_bitmask, WeatherEntry::BITMASK_SIZE_BYTES);
    // --- End of Current Entry Creation ---


    // --- Initialize State Variables ---
    WiFiManager wifiManager(Config::WIFI_SSID, Config::WIFI_PASSWORD);
    MqttClientWrapper mqttClient(Config::MQTT_SERVER_IP, Config::MQTT_PORT,
                                 Config::MQTT_CLIENT_ID_PREFIX, Config::MQTT_TOPIC_STATUS);
    const ProtoDataFormatter formatter{};

    bool wifiConnected = false;
    bool mqttConnected = false;
    bool currentEntrySentSuccessfully = false; // Track if the CURRENT entry was sent
    uint32_t oldestSuccessfulPruneTs = 0; // Track oldest data successfully sent *this cycle*

    // --- 2. Attempt Network Connections and Publishing ---
    if (wifiManager.connect(Config::WIFI_CONNECT_TIMEOUT_MS)) {
        wifiConnected = true;
        Serial.println("WiFi connected successfully.");

        // Optional: Sync Time (Best Effort)
        if (timeManager.syncTimeWithNTP(Config::NTP_TIMEOUT_MS)) {
            const time_t syncedTime = timeManager.getRTCEpochTime();
            timeManager.setInternalRTC(syncedTime);
            Serial.print("Time synced. Current time: ");
            timeManager.printFormattedTime(syncedTime);
        } else {
            Serial.print("NTP Time sync failed. Continuing with RTC time: ");
            timeManager.printFormattedTime(timeManager.getRTCEpochTime());
        }

        // Attempt MQTT Connection
        if (mqttClient.connect()) {
            mqttConnected = true;
            Serial.println("MQTT connected successfully.");

            // --- MQTT Operations ---

            // A. Retry sending failed entries from the queue
            Serial.printf("Attempting to send %d queued entries...\n", failedEntriesQueue.getCount());
            size_t processedCount = 0;
            const size_t initialQueueCount = failedEntriesQueue.getCount();

            while (processedCount < initialQueueCount && !failedEntriesQueue.isEmpty()) {
                processedCount++;
                WeatherEntry entryToRetry;
                if (!failedEntriesQueue.peek(entryToRetry)) break; // Safety check

                if (attemptPublish(mqttClient, entryToRetry, formatter, timeManager)) {
                    const uint32_t queued_entry_start_ts = WeatherEntry::calcStartTime(entryToRetry.timestamp_s);
                    if (oldestSuccessfulPruneTs == 0 || queued_entry_start_ts < oldestSuccessfulPruneTs) {
                        oldestSuccessfulPruneTs = queued_entry_start_ts;
                    }
                    failedEntriesQueue.pop(entryToRetry); // Remove on success
                    Serial.println("  -> Queued entry removed.");
                } else {
                    Serial.println("  -> Oldest queued entry failed to send. Stopping retry attempts.");
                    break; // Stop retrying if the oldest fails
                }
                 delay(50); // Small delay between publishes if needed
            }
            Serial.printf("Finished processing %d queued entries.\n", processedCount);


            // B. Attempt to send the CURRENT entry (already created)
            Serial.println("Attempting to send current entry...");
            if (attemptPublish(mqttClient, current_weather_entry, formatter, timeManager)) {
                currentEntrySentSuccessfully = true; // Mark as sent
                // Update pruning timestamp if needed
                if (oldestSuccessfulPruneTs == 0 || entry_coverage_start_ts < oldestSuccessfulPruneTs) {
                    oldestSuccessfulPruneTs = entry_coverage_start_ts;
                }
            } // If it fails, currentEntrySentSuccessfully remains false

            // Disconnect MQTT after operations
            delay(100); // Allow time for MQTT ack/processing
            mqttClient.disconnect();
            Serial.println("MQTT disconnected.");
            mqttConnected = false; // Update state

        } else { // MQTT connection failed
             Serial.println("MQTT connection failed.");
             // currentEntrySentSuccessfully remains false
        }
    } else { // WiFi connection failed
        Serial.println("WiFi connection failed.");
        // currentEntrySentSuccessfully remains false
    }

    // --- 3. Queue Current Entry IF it wasn't sent ---
    if (!currentEntrySentSuccessfully) {
        Serial.println("Current weather entry was not sent successfully. Adding to queue.");
        if (!failedEntriesQueue.push(current_weather_entry)) {
             // Ouch, queue is full AND we couldn't send the current one. Oldest *failed* entry gets dropped.
             Serial.println("!!! WARNING: Failed entry queue is full! Could not add current entry. Oldest failed entry was dropped. !!!");
        } else {
             Serial.printf("Current entry added to queue. Queue size: %d/%d\n",
                           failedEntriesQueue.getCount(), failedEntriesQueue.getCapacity());
        }
    } else {
         Serial.println("Current weather entry was sent successfully.");
    }


    // --- 4. Pruning Rain Sensor Data (Conditional) ---
    if (oldestSuccessfulPruneTs > 0) {
        Serial.print("Pruning RainSensor events older than timestamp: ");
        timeManager.printFormattedTime(oldestSuccessfulPruneTs);
        // !!! IMPORTANT: Requires RainSensor::pruneEventsOlderThan(uint32_t timestamp) implementation !!!
        // RainSensor::pruneEventsOlderThan(oldestSuccessfulPruneTs);
        Serial.println("NOTE: RainSensor event pruning needs implementation in RainSensor class.");
    } else {
         Serial.println("No reports sent successfully this cycle. Skipping RainSensor event pruning.");
    }


    // --- 5. Cleanup and Sleep ---
    if (wifiConnected) { // Disconnect WiFi if we connected it
        wifiManager.disconnect();
        Serial.println("WiFi disconnected.");
    }

    Serial.println("--- Reporting Cycle Ended ---");
    Serial.println("Configuring sleep (Timer + GPIO)...");
    rainSensor.setupWakeupPin();
    esp_sleep_enable_timer_wakeup(Config::REPORT_INTERVAL_US);
    Serial.println("Entering deep sleep...");
    delay(100);
    esp_deep_sleep_start();

    // Should not reach here
    Serial.println("!!! ERROR: Code continued after deep sleep call? !!!");
}

// Sync Time function (can remain as is, or simplified if only needed at boot)
void syncTime(TimeManager& timeManager) {
    WiFiManager wifiManager(Config::WIFI_SSID, Config::WIFI_PASSWORD);
    if (wifiManager.connect(Config::WIFI_CONNECT_TIMEOUT_MS)) {
        Serial.println("WiFi connected for time sync.");
        if (timeManager.syncTimeWithNTP(Config::NTP_TIMEOUT_MS)) {
            const time_t syncedTime = timeManager.getRTCEpochTime();
            timeManager.setInternalRTC(syncedTime);
            Serial.print("Time synced successfully. Current time: ");
            timeManager.printFormattedTime(syncedTime);
        } else {
            Serial.println("NTP Time sync failed.");
        }
        wifiManager.disconnect();
        Serial.println("WiFi disconnected after time sync attempt.");
    } else {
        Serial.println("WiFi connection failed during time sync attempt.");
    }
}

// --- Setup and Loop (largely unchanged from previous version) ---

void setup() {
    delay(500);
    Serial.begin(115200);
    delay(100);

    Serial.println("\n\n--- ESP32 Weather Station Booting ---");

    TimeManager timeManager;
    Serial.print("Current Time (RTC on boot): ");
    timeManager.printFormattedTime(timeManager.getRTCEpochTime());

    const RainSensor rainSensor(Config::RAIN_SENSOR_PIN);

    const esp_sleep_wakeup_cause_t wakeup_reason = esp_sleep_get_wakeup_cause();

    switch(wakeup_reason) {
        case ESP_SLEEP_WAKEUP_EXT0: // Rain Tip GPIO
            {
                Serial.println("Wakeup Source: Rain Tip GPIO");
                const time_t now = timeManager.getRTCEpochTime();
                if (TimeManager::isTimeValid(now)) {
                    Serial.print("Logging tip event at: ");
                    timeManager.printFormattedTime(now);
                    RainSensor::handleTipWakeupEvent(now); // Must persist event
                } else {
                    Serial.println("RTC time seems invalid, tip not logged reliably.");
                }
                Serial.println("Configuring sleep (Timer + GPIO)...");
                rainSensor.setupWakeupPin();
                esp_sleep_enable_timer_wakeup(Config::REPORT_INTERVAL_US);
                Serial.println("Entering deep sleep...");
                delay(100);
                esp_deep_sleep_start();
                break;
            }
        case ESP_SLEEP_WAKEUP_TIMER: // Reporting Interval
            {
                 // Check/Initialize queue only on timer wakeups (where sendRaport runs)
                 if (!failedEntriesQueue.isInitialized()) {
                     Serial.println("RTC Data invalid/First Boot: Initializing failed entries queue.");
                     failedEntriesQueue.reset();
                 }
                 Serial.printf("Failed entry queue status on wakeup: %d/%d used.\n",
                              failedEntriesQueue.getCount(), failedEntriesQueue.getCapacity());

                sendRaport(timeManager, rainSensor); // Handles reporting, queueing, and sleep
                break;
            }
        default: // Reset, Power-On, or Other Wakeup Source
            {
                Serial.printf("Wakeup Source: Other/Reset (%d)\n", wakeup_reason);
                Serial.println("Performing initial setup/reset tasks...");
                syncTime(timeManager); // Attempt time sync

                 Serial.println("Initializing/Resetting failed entries queue.");
                 failedEntriesQueue.reset(); // Clear queue on reset

                Serial.println("Initializing/Resetting RainSensor persistent data.");
                RainSensor::resetPersistedData(timeManager.getRTCEpochTime()); // Reset sensor data

                Serial.println("Configuring sleep (Timer + GPIO)...");
                rainSensor.setupWakeupPin();
                esp_sleep_enable_timer_wakeup(Config::REPORT_INTERVAL_US);
                Serial.println("Initial boot or Reset complete. Entering deep sleep...");
                delay(100);
                esp_deep_sleep_start();
                break;
            }
    }
}

void loop() {
    Serial.println("!!! ERROR: Reached loop() - Deep sleep logic failed? !!!");
    delay(10000);
    esp_sleep_enable_timer_wakeup(Config::REPORT_INTERVAL_US);
    esp_deep_sleep_start();
}