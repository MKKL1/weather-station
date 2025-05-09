#ifndef STATUSLED_H
#define STATUSLED_H
#include <Arduino.h>

class StatusLED {
public:
    explicit StatusLED(uint8_t ledPin) : _pin(ledPin) {
        pinMode(_pin, OUTPUT);
        off();
    }
    void on() const { digitalWrite(_pin, HIGH);  }
    void off() const { digitalWrite(_pin, LOW);   }
private:
    uint8_t _pin;
};

#endif //STATUSLED_H
