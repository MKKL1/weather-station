//
// Created by krystian on 12.05.2025.
//

#ifndef INSTANCEIDGEN_H
#define INSTANCEIDGEN_H

#include <cstdint>
#include <esp_system.h>
#include <esp_attr.h>

// Class to generate a unique Instance ID per power-on reset
// Provides exactly one ID per reset by checking the retained variable
class InstanceIdGen {
public:
    // Returns the unique instance ID
    static uint32_t getInstanceId() {
        if (s_instanceId == 0) {
            // First call after reset: generate a random 32-bit ID
            s_instanceId = esp_random();
        }
        return s_instanceId;
    }

    InstanceIdGen() = delete;
    ~InstanceIdGen() = delete;
    InstanceIdGen(const InstanceIdGen&) = delete;
    InstanceIdGen& operator=(const InstanceIdGen&) = delete;
private:
    RTC_DATA_ATTR static uint32_t s_instanceId;
};


#endif //INSTANCEIDGEN_H
