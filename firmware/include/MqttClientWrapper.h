// MqttClientWrapper.h
#ifndef MQTT_CLIENT_WRAPPER_H
#define MQTT_CLIENT_WRAPPER_H

#include <WiFi.h>
#include <PubSubClient.h>
#include "CircularQueue.h"
#include "Config.h"

namespace MQTT {

    // Max size for a queued MQTT message payload
    static constexpr size_t MAX_PAYLOAD_SIZE = Config::MQTT_MSG_BUFFER_SIZE;
    // Max length for topic strings
    static constexpr size_t MAX_TOPIC_LENGTH = 64;

    struct Message {
        char topic[MAX_TOPIC_LENGTH];
        uint8_t payload[MAX_PAYLOAD_SIZE];
        uint16_t length;
    };

    class Client {
    public:
        Client(const char* serverIp, uint16_t port, const char* clientIdPrefix);

        /**
         * Connects to MQTT broker.
         */
        bool connect(uint32_t timeoutMs = 5000);

        /**
         * Disconnects from broker.
         */
        void disconnect();

        /**
         * Publish, and enqueue on failure.
         */
        bool publish(const char* topic, const uint8_t* payload, uint16_t length);

        /**
         * Retry sending any queued messages.
         */
        void retryQueued();

    private:
        WiFiClient m_wifiClient;
        PubSubClient m_mqtt;
        char m_clientId[64]{};

        // Persistent queue of failed publishes
        static RTC_DATA_ATTR CircularQueue<Message, Config::FAILED_MQTT_QUEUE_SIZE> m_queue;

        static void enqueue_(const char* topic, const uint8_t* payload, uint16_t length);
        bool sendMessage_(const Message& msg);
    };

} // namespace MQTT

#endif // MQTT_CLIENT_WRAPPER_H