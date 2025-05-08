#ifndef MQTT_CLIENT_WRAPPER_H
#define MQTT_CLIENT_WRAPPER_H

#include <Wifi.h>
#include <PubSubClient.h>

class MqttClientWrapper {
private:
    WiFiClient m_wifiClient;
    PubSubClient m_mqttClient;
    const char* m_serverIp;
    uint16_t m_port;
    const char* m_clientIdPrefix;
    const char* m_statusTopic;
    char m_clientId[64];
    bool m_connected = false;
    unsigned long m_lastAttemptTime = 0;

public:
    MqttClientWrapper(const char* serverIp, uint16_t port, const char* clientIdPrefix, const char* statusTopic)
        : m_mqttClient(m_wifiClient), m_serverIp(serverIp), m_port(port),
          m_clientIdPrefix(clientIdPrefix), m_statusTopic(statusTopic)
    {
        m_mqttClient.setServer(m_serverIp, m_port);
    }

    bool connect(const uint32_t timeoutMs = 5000) {
        if (WiFi.status() != WL_CONNECTED) {
             Serial.println("MQTT Error: Cannot connect, WiFi not available.");
             m_connected = false;
             return false;
        }

        if (m_mqttClient.connected()) {
             if (!m_connected) {
                 Serial.println("MQTT Warning: Client connected but internal flag was false. Syncing.");
                 m_connected = true;
             }
            return true;
        }

        if (m_connected) {
             Serial.println("MQTT Warning: Internal flag true, but client disconnected. Syncing.");
             m_connected = false;
        }

        Serial.print("Attempting MQTT connection to "); Serial.print(m_serverIp); Serial.print("...");
        uint64_t mac = ESP.getEfuseMac();
        snprintf(m_clientId, sizeof(m_clientId), "%s%08X", m_clientIdPrefix, (uint32_t)(mac >> 16)); // Use part of MAC

        unsigned long startAttempt = millis();
        while (!m_mqttClient.connected() && millis() - startAttempt < timeoutMs) {
            if (m_mqttClient.connect(m_clientId)) {
                Serial.println(" connected!");
                Serial.print("  Client ID: "); Serial.println(m_clientId);
                m_connected = true;
                return true;
            } else {
                Serial.print("\n  Failed, rc="); Serial.print(m_mqttClient.state());
                Serial.print(". Retrying in 500ms...");
                m_lastAttemptTime = millis();
                delay(500);
            }
        }

        Serial.println("\nMQTT connection failed after timeout.");
        m_connected = false;
        return false;
    }

    void disconnect() {
        if (m_connected) {
             m_mqttClient.disconnect();
        }
        m_connected = false;
    }

    bool publish(const char* topic, const uint8_t* payload, const unsigned int length) {
        if (!isConnected()) {
            Serial.println("MQTT Publish Error: Not connected.");
            return false;
        }
        Serial.print("MQTT Publish ["); Serial.print(topic); Serial.print("] ");

        if (m_mqttClient.publish(topic, payload, length)) {
            Serial.println("  Success.");
            return true;
        } else {
            Serial.println("  FAILED!");
            if (!m_mqttClient.connected()) {
                Serial.println("  Reason: MQTT client disconnected.");
                m_connected = false;
            }
            return false;
        }
    }

    void loop() {
        if (!WiFi.isConnected()) {
            if (m_connected) {
                 Serial.println("MQTT loop: WiFi disconnected, marking MQTT as disconnected.");
                 m_connected = false;
                 m_mqttClient.disconnect();
            }
            return;
        }

        if (m_connected) {
            if (!m_mqttClient.loop()) {
                 Serial.println("MQTT loop detected disconnection.");
                 m_connected = false;
            }
        }
    }

    bool isConnected() {
        if (m_connected && !m_mqttClient.connected()) {
            Serial.println("isConnected() detected external disconnect. Updating state.");
            m_connected = false;
        }
        if (m_connected && WiFi.status() != WL_CONNECTED) {
            Serial.println("isConnected() detected WiFi disconnect. Updating state.");
            m_connected = false;
            m_mqttClient.disconnect();
        }
        return m_connected;
    }
};
#endif // MQTT_CLIENT_WRAPPER_H