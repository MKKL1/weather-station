#ifndef WEATHERENTRY_H
#define WEATHERENTRY_H

#include <cstdint>

struct  WeatherEntry {
    //This configuration gives exactly 1 interval outside 30 minute range
    static constexpr uint32_t ENTRY_DURATION = 1920u; //32 minutes
    static constexpr size_t NUM_INTERVALS_PER_ENTRY = 16; //every 2 minutes
    static constexpr size_t BITS_PER_INTERVAL = 4; //count up to 15

    static constexpr size_t BITMASK_SIZE_BYTES = (NUM_INTERVALS_PER_ENTRY * BITS_PER_INTERVAL + 7) / 8;
    static constexpr uint8_t MAX_TIPS_PER_INTERVAL = (1 << BITS_PER_INTERVAL) - 1;
    static constexpr uint32_t INTERVAL_DURATION_S = ENTRY_DURATION / NUM_INTERVALS_PER_ENTRY;

    uint32_t timestamp_s = 0;
    uint8_t temperature = 0;
    uint8_t pressure = 0;
    uint8_t humidity = 0;
    uint8_t tip_bitmask[BITMASK_SIZE_BYTES] = {0};

    // Clear all tip counts in the bitmask
    void clearBitmask() {
        memset(tip_bitmask, 0, sizeof(tip_bitmask));
    }

    // Set the tip count for a specific interval (0-23)
    bool setTipCount(size_t interval_index, uint8_t count) {
        if (interval_index >= NUM_INTERVALS_PER_ENTRY) return false;
        if (count > MAX_TIPS_PER_INTERVAL) count = MAX_TIPS_PER_INTERVAL;

        size_t byte_index = interval_index / 2;
        bool is_high_nibble = (interval_index % 2 != 0);
        uint8_t clear_mask = is_high_nibble ? 0xF0 : 0x0F;
        uint8_t value_to_set = is_high_nibble ? (count << 4) : count;

        tip_bitmask[byte_index] = (tip_bitmask[byte_index] & ~clear_mask) | value_to_set;
        return true;
    }

    // Get the tip count for a specific interval (0-23)
    [[nodiscard]] uint8_t getTipCount(const size_t interval_index) const {
        if (interval_index >= NUM_INTERVALS_PER_ENTRY) return 0;

        const size_t byte_index = interval_index / 2;
        const bool is_high_nibble = (interval_index % 2 != 0);
        const uint8_t count = tip_bitmask[byte_index];

        return (is_high_nibble ? (count >> 4) : count) & 0x0F;
    }

    bool incrementTipCount(const uint32_t event_ts, const uint32_t time_now) {
        const uint32_t aligned_window_end_ts = (time_now / INTERVAL_DURATION_S) * INTERVAL_DURATION_S;
        const uint32_t entry_coverage_start_ts = aligned_window_end_ts - ENTRY_DURATION;

        if (event_ts < entry_coverage_start_ts || event_ts >= aligned_window_end_ts) {
            // throw std::out_of_range("event_ts outside of [timestamp_s, timestamp_s + 1h)");
            return false;
        }
        const uint32_t offset = event_ts - entry_coverage_start_ts;
        const size_t   idx    = offset / INTERVAL_DURATION_S;
        const uint8_t  cur    = getTipCount(idx);
        if (cur >= MAX_TIPS_PER_INTERVAL) {
            // throw std::overflow_error("tip count overflow in interval");
            return false;
        }
        setTipCount(idx, cur + 1);

        return true;
    }
};
#endif //WEATHERENTRY_H
