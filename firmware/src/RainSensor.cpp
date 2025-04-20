#include "RainSensor.h"

void RainSensor::setupWakeupPin() const {
    // Ensure pin is configured correctly (e.g., INPUT_PULLUP) BEFORE enabling wakeup
    // This prevents spurious wakeups immediately after configuration.
    pinMode(m_pin, INPUT_PULLUP);
    delay(50); // Small delay might help ensure pull-up is stable

    Serial.printf("Configuring GPIO %d for deep sleep wakeup (EXT0, falling edge)\n", m_pin);
    // Wake up when the pin goes LOW (0 = FALLING edge for pull-up sensor)
    esp_err_t err = esp_sleep_enable_ext0_wakeup(m_pin, 0);
    if (err != ESP_OK) {
        Serial.printf("ERROR configuring EXT0 wakeup: %s\n", esp_err_to_name(err));
    }
}

void RainSensor::handleTipWakeupEvent(const time_t wakeupTime) {
    constexpr time_t DEBOUNCE_TIME_SEC = 5;

    Serial.printf("s_lastTipTimestamp: %d, wakeupTime: %d", s_lastTipTimestamp, wakeupTime);
    if (wakeupTime - s_lastTipTimestamp >= DEBOUNCE_TIME_SEC || s_lastTipTimestamp == 0) {
        s_lastTipTimestamp = wakeupTime;
        queue.push(static_cast<uint16_t>((s_lastResetTimestamp - wakeupTime)/60));
    } else {
        Serial.printf("RainSensor: Tip ignored (debounce). Last tip: %lu, Current: %lu\n",
                    static_cast<unsigned long>(s_lastTipTimestamp), static_cast<unsigned long>(wakeupTime));
    }
}

void RainSensor::resetPersistedData(const time_t resetTime) {
    Serial.println("RainSensor::resetPersistedData");
    s_lastResetTimestamp = resetTime;
    s_lastTipTimestamp = 0;
}

// --- Initialize Static RTC Variables ---
// These definitions allocate the memory in the RTC slow RAM segment.
RTC_DATA_ATTR time_t RainSensor::s_lastResetTimestamp = 0;
RTC_DATA_ATTR time_t RainSensor::s_lastTipTimestamp = 0;
RTC_DATA_ATTR CircularQueue<uint16_t, RainSensor::CIRCULAR_QUEUE_SIZE> RainSensor::queue;