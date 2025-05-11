// MqttClientWrapper.cpp
#include "MqttClientWrapper.h"
#include <esp_mac.h>
#include <esp_sleep.h>

// Uncomment for debug
#define MQTT_DEBUG
#ifdef MQTT_DEBUG
  #define MQTT_LOG(...) Serial.printf(__VA_ARGS__)
  #define MQTT_LOGLN(...) Serial.println(__VA_ARGS__)
#else
  #define MQTT_LOG(...)
  #define MQTT_LOGLN(...)
#endif

namespace MQTT {

// Define static RTC-persistent queue
RTC_DATA_ATTR CircularQueue<Message, Config::FAILED_MQTT_QUEUE_SIZE> Client::m_queue;

Client::Client(const char* serverIp, const uint16_t port, const char* clientIdPrefix)
    : m_mqtt(m_wifiClient)
{
    MQTT_LOG("Creating MQTT client to %s:%d", serverIp , port);
    m_mqtt.setServer(serverIp, port);
    // build clientId from prefix + MAC
    uint8_t mac[6];
    esp_read_mac(mac, ESP_MAC_WIFI_STA);
    snprintf(m_clientId, sizeof(m_clientId), "%s%02X%02X%02X",
             clientIdPrefix, mac[3], mac[4], mac[5]);
}

bool Client::connect(const uint32_t timeoutMs) {
    if (WiFiClass::status() != WL_CONNECTED) {
        MQTT_LOGLN("MQTT: WiFi not connected, cannot connect.");
        return false;
    }
    unsigned long start = millis();
    while (!m_mqtt.connected() && millis() - start < timeoutMs) {
        if (m_mqtt.connect(m_clientId)) {
            MQTT_LOG("MQTT: connected as %s", m_clientId);
            return true;
        }
        MQTT_LOGLN("MQTT: connect failed, retrying...");
        delay(500);
    }
    MQTT_LOGLN("MQTT: connection timeout.");
    return m_mqtt.connected();
}

void Client::disconnect() {
    if (m_mqtt.connected()) m_mqtt.disconnect();
}

bool Client::publish(const char* topic, const uint8_t* payload, const uint16_t length) {
    if (!m_mqtt.connected()) {
        MQTT_LOGLN("MQTT: not connected, queuing message.");
        enqueue_(topic, payload, length);
        return false;
    }
    if (m_mqtt.publish(topic, payload, length)) {
        return true;
    }
    MQTT_LOGLN("MQTT: publish failed, queuing message.");
    enqueue_(topic, payload, length);
    return false;
}

void Client::enqueue_(const char* topic, const uint8_t* payload, uint16_t length) {
    Message msg{};
    strncpy(msg.topic, topic, MAX_TOPIC_LENGTH - 1);
    msg.length = length > MAX_PAYLOAD_SIZE ? MAX_PAYLOAD_SIZE : length;
    memcpy(msg.payload, payload, msg.length);
    m_queue.push(msg);
}

void Client::retryQueued() {
    if (!m_mqtt.connected()) return;
    const size_t initial = m_queue.getCount();
    for (size_t i = 0; i < initial; ++i) {
        Message msg{};
        if (!m_queue.peek(msg)) break;
        if (sendMessage_(msg)) {
            m_queue.pop();
        } else {
            // stop on first failure
            break;
        }
    }
}

bool Client::sendMessage_(const Message& msg) {
    return m_mqtt.publish(msg.topic, msg.payload, msg.length);
}

} // namespace MQTT
