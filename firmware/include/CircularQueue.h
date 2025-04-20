#ifndef CIRCULARQUEUE_H
#define CIRCULARQUEUE_H

#include <array>
#include <cstddef>
#include <vector>

template <typename T, size_t Size>
class CircularQueue {
    static_assert(Size > 0, "Buffer size must be greater than 0");

private:
    std::array<T, Size> buffer{};
    size_t head = 0;
    size_t tail = 0;
    size_t count = 0;
    bool initialized = false;

public:
    void reset() {
        head = tail = count = 0;
        std::fill(buffer.begin(), buffer.end(), T{});
        initialized = true;
    }

    [[nodiscard]] bool isInitialized() const { return initialized; }
    [[nodiscard]] bool isEmpty()        const { return count == 0; }
    [[nodiscard]] bool isFull()         const { return count == Size; }
    [[nodiscard]] size_t getCount()     const { return count; }
    [[nodiscard]] size_t getCapacity()  const { return Size; }

    bool push(const T& item) {
        if (isFull()) {
            // drop oldest to make room
            tail = (tail + 1) % Size;
            --count;
        }
        buffer[head] = item;
        head = (head + 1) % Size;
        ++count;
        return true;
    }

    bool pop(T& item) {
        if (isEmpty()) return false;
        item = buffer[tail];
        tail = (tail + 1) % Size;
        --count;
        return true;
    }

    bool peek(T& item) const {
        if (isEmpty()) return false;
        item = buffer[tail];
        return true;
    }

    const T& operator[](const size_t i) const {
        if (i >= count) throw std::out_of_range("CircularQueue index");
        return buffer[(tail + i) % Size];
    }

    std::vector<T> toArray() const {
        std::vector<T> v;
        v.reserve(count);
        for (size_t i = 0; i < count; ++i) {
            v.push_back(buffer[(tail + i) % Size]);
        }
        return v;
    }

    // ── new: try to process the oldest element in-place ──
    // If processor(item) returns true, remove it; otherwise leave it for retry.
    template <typename Processor>
    bool processNext(Processor&& processor) {
        if (isEmpty()) return false;
        T& item = buffer[tail];
        if (std::forward<Processor>(processor)(item)) {
            // only pop on success
            tail = (tail + 1) % Size;
            --count;
            return true;
        }
        return false;
    }
};




#endif //CIRCULARQUEUE_H
