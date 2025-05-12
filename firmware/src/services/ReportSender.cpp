#include "services/ReportSender.h"
#include <Arduino.h>
#include <ProtoDataFormatter.h>
#include <WeatherData.h>

// Uncomment for debug
#define RS_DEBUG
#ifdef RS_DEBUG
  #define RS_LOG(...) Serial.printf(__VA_ARGS__)
  #define RS_LOGLN(...) Serial.println(__VA_ARGS__)
#else
  #define RS_LOG(...)
  #define RS_LOGLN(...)
#endif

inline void printBitmask(const uint8_t *mask, size_t len) {
    for (size_t i = 0; i < len; i++) {
        if (mask[i] < 0x10) Serial.print('0');
        Serial.print(mask[i], HEX);
    }
    Serial.println();
}

ReportSender::ReportSender(const char* mqttServer,
                           const uint16_t mqttPort,
                           const char* chipId,
                           const char* mqttTopic,
                           const float mmpt,
                           const uint32_t instanceId)
    : _mqtt(mqttServer, mqttPort, Config::MQTT_CLIENT_ID_PREFIX), _chipId(chipId), _mqttTopic(mqttTopic), _mmpt(mmpt), _instanceId(instanceId) {
}

void ReportSender::send(const WeatherEntry& currentEntry) {
    connectServices();
    if (_mqttConnected) {
        retryQueued();
        publishEntry(currentEntry);
        _mqtt.disconnect();
    } else {
        // enqueue via wrapper even if never connected
        publishEntry(currentEntry);
    }
    // WiFiManager::disconnect();
}

void ReportSender::connectServices() {
    // if (!WiFiManager::connect(Config::WIFI_SSID, Config::WIFI_PASSWORD, Config::WIFI_CONNECT_TIMEOUT_MS)) {
    //     Serial.println("WiFi connect failed");
    //     return;
    // }
    // Serial.println("WiFi connected");

    if (!_mqtt.connect()) {
        Serial.println("MQTT connect failed");
    } else {
        Serial.println("MQTT connected");
        _mqttConnected = true;
    }
}

void ReportSender::retryQueued() {
    Serial.println("Retrying queued MQTT messages...");
    _mqtt.retryQueued();
}

void ReportSender::publishEntry(const WeatherEntry& entry) {
    Serial.printf("Instance id: %u \n", _instanceId);

    uint8_t buffer[Config::MQTT_MSG_BUFFER_SIZE];
    const size_t len = ProtoDataFormatter{}.formatData(
        WeatherData(entry, DeviceInfo(_chipId, _mmpt, _instanceId)),
        buffer,
        sizeof(buffer)
    );

    #ifdef RS_DEBUG
    Serial.println("Sending message: ");
    printBitmask(buffer, len);
    #endif

    if (len == 0) {
        Serial.println("Formatting failed");
        return;
    }
    if (_mqtt.publish(_mqttTopic, buffer, len)) {
        Serial.println("Published entry");
    } else {
        Serial.println("Publish failed, queued for retry");
    }
}