#ifndef RAIN_SENSOR_H
#define RAIN_SENSOR_H

#include <CircularQueue.h>
#include "Arduino.h"

class RainSensor {
    constexpr static  size_t CIRCULAR_QUEUE_SIZE = 256;
    static RTC_DATA_ATTR time_t s_lastTipTimestamp;
    static RTC_DATA_ATTR time_t s_lastResetTimestamp; //SET ON FIRST BOOT
    static RTC_DATA_ATTR CircularQueue<uint16_t, CIRCULAR_QUEUE_SIZE> queue;
    gpio_num_t m_pin;

public:
    explicit RainSensor(const gpio_num_t sensorPin) : m_pin(sensorPin) {}

    void setupWakeupPin() const;

    static void handleTipWakeupEvent(time_t wakeupTime);

    static void resetPersistedData(time_t resetTime);

    class EventTimeIterator {
    public:
        using iterator_category = std::forward_iterator_tag;
        using value_type        = time_t;
        using difference_type   = std::ptrdiff_t;
        using pointer           = time_t*;
        using reference         = time_t&;

        explicit EventTimeIterator(const size_t idx) : idx_{idx} {}

        time_t operator*() const {
            // offset in seconds stored at logical index idx_
            const uint16_t offset = queue[idx_];
            return s_lastResetTimestamp + offset;
        }

        EventTimeIterator& operator++() {
            ++idx_;
            return *this;
        }

        bool operator!=(EventTimeIterator const& o) const {
            return idx_ != o.idx_;
        }

    private:
        size_t idx_;
    };

    struct EventTimeRange {
        [[nodiscard]] EventTimeIterator begin() const {
            return EventTimeIterator{0};
        }
        [[nodiscard]] EventTimeIterator end() const {
            return EventTimeIterator{ queue.getCount() };
        }
    };

    // call this to iterate:
    static EventTimeRange events() {
        return {};
    }
};

#endif // RAIN_SENSOR_H