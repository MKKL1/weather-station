#include "RainSensor.h"
#include <esp_sleep.h>
#include <esp_err.h>

//#define RAIN_DEBUG
#ifdef RAIN_DEBUG
  #define RAIN_LOG(...) Serial.printf(__VA_ARGS__)
  #define RAIN_LOGLN(...) Serial.println(__VA_ARGS__)
#else
  #define RAIN_LOG(...)
  #define RAIN_LOGLN(...)
#endif

namespace RainSensor {

// Initialize static RTC vars
RTC_DATA_ATTR time_t Sensor::s_lastTip = 0;
RTC_DATA_ATTR CircularQueue<time_t, Config::MAX_RAIN_EVENTS> Sensor::s_queue;

Sensor::Sensor(gpio_num_t pin) : m_pin(pin) {}

void Sensor::begin() const {
    pinMode(m_pin, INPUT_PULLUP);
    delay(50);
    configureWakeup_();
}

void Sensor::configureWakeup_() const {
    esp_sleep_enable_ext0_wakeup(m_pin, 0);
    RAIN_LOG("RainSensor: wakeup on GPIO %d\n", m_pin);
}

bool Sensor::handleTip(const time_t now) {
    if (s_lastTip != 0 && now - s_lastTip < DEBOUNCE_SEC) {
        RAIN_LOGLN("RainSensor: tip ignored (debounce)");
        return false;
    }
    if (s_queue.isFull()) {
        RAIN_LOGLN("RainSensor: queue full, overwriting oldest");
    }
    s_queue.push(now);
    s_lastTip = now;
    RAIN_LOG("RainSensor: tip recorded at %lu\n", static_cast<unsigned long>(now));
    return true;
}

void Sensor::reset(const time_t baseline) {
    s_queue.reset();
    s_lastTip = baseline;
    RAIN_LOGLN("RainSensor: data reset at baseline %lu", static_cast<unsigned long>(baseline));
}

// Iterator definitions
Sensor::Iterator::Iterator(size_t idx) : idx_(idx) {}

time_t Sensor::Iterator::operator*() const {
    return s_queue[idx_];
}

Sensor::Iterator& Sensor::Iterator::operator++() {
    ++idx_;
    return *this;
}

bool Sensor::Iterator::operator!=(const Iterator& other) const {
    return idx_ != other.idx_;
}

Sensor::Range Sensor::events() {
    return Range{};
}

Sensor::Iterator Sensor::Range::begin() const {
    return Iterator{0};
}

Sensor::Iterator Sensor::Range::end() const {
    return Iterator{ s_queue.getCount() };
}

} // namespace RainSensor
