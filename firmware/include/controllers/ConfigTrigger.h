//
// Created by krystian on 09.05.2025.
//

#ifndef CONFIGTRIGGER_H
#define CONFIGTRIGGER_H
#include <Arduino.h>

class ConfigTrigger {
public:
    explicit ConfigTrigger(const uint8_t buttonPin) : _pin(buttonPin) {
        pinMode(_pin, INPUT_PULLUP);
    }
    /// Call at VERY start of boot (before WiFi).
    /// Returns true if we should launch AP portal.
    [[nodiscard]] bool shouldEnterConfig() const {
        // LOW == pressed
        return digitalRead(_pin) == LOW;
    }
private:
    uint8_t _pin;
};


#endif //CONFIGTRIGGER_H
