// RainSensor.h
#ifndef RAIN_SENSOR_H
#define RAIN_SENSOR_H

#include <ctime>
#include <Arduino.h>
#include "CircularQueue.h"
#include "Config.h"

namespace RainSensor {

    /**
     * Rain sensor abstraction using a circular queue in RTC memory.
     * Records tip event timestamps and provides access to historic events.
     */
    class Sensor {
    public:
        explicit Sensor(gpio_num_t pin);

        /**
         * Configure the sensor pin and enable wakeup.
         * Call once in setup().
         */
        void begin() const;

        /**
         * Record a tip event at the given time (RTC epoch seconds).
         * Returns true if the event was recorded (debounced).
         */
        bool handleTip(time_t now);

        /**
         * Clear all persisted events and reset timestamps.
         * Call on first boot or reset.
         */
        void reset(time_t baseline);

        /**
         * Iterator over recorded event times.
         */
        class Iterator {
        public:
            Iterator(size_t idx);
            time_t operator*() const;
            Iterator& operator++();
            bool operator!=(const Iterator& other) const;
        private:
            size_t idx_;
        };

        /**
         * Range type for iterating events.
         */
        struct Range {
            Iterator begin() const;
            Iterator end() const;
        };

        /**
         * Accessor for event range.
         */
        static Range events();

    private:
        gpio_num_t m_pin;
        static const time_t DEBOUNCE_SEC = 5;

        // RTC-persistent storage
        static RTC_DATA_ATTR time_t s_lastTip;
        static RTC_DATA_ATTR CircularQueue<time_t, Config::MAX_RAIN_EVENTS> s_queue;

        void configureWakeup_() const;
    };

} // namespace RainSensor

#endif // RAIN_SENSOR_H