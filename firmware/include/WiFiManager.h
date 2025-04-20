#ifndef WIFI_MANAGER_H
#define WIFI_MANAGER_H

#include <Wifi.h>

class WiFiManager {
private:
    const char* m_ssid;
    const char* m_password;
    bool m_connected = false;

public:
    WiFiManager(const char* ssid, const char* password)
        : m_ssid(ssid), m_password(password) {}

    bool connect(uint32_t timeoutMs) {
        if (WiFi.status() == WL_CONNECTED) {
             m_connected = true;
             Serial.println("WiFi already connected.");
             printStatus();
             return true;
        }

        WiFi.mode(WIFI_STA);
        WiFi.begin(m_ssid, m_password);
        Serial.print("Connecting to WiFi "); Serial.print(m_ssid);

        unsigned long startAttemptTime = millis();
        while (WiFi.status() != WL_CONNECTED && millis() - startAttemptTime < timeoutMs) {
            Serial.print(".");
            delay(500);
        }

        if (WiFi.status() == WL_CONNECTED) {
            m_connected = true;
            Serial.println("\nWiFi connected!");
            printStatus();
            return true;
        } else {
            m_connected = false;
            Serial.println("\nWiFi connection failed!");
            WiFi.disconnect(true);
             WiFi.mode(WIFI_OFF);
            return false;
        }
    }

    void disconnect() {
        if (m_connected) {
            Serial.println("Disconnecting WiFi...");
            WiFi.disconnect(true);
             WiFi.mode(WIFI_OFF);
        }
        m_connected = false;
    }

    bool isConnected() const {
        return m_connected;
    }

    void printStatus() const {
         if (m_connected) {
            Serial.print("  IP Address: "); Serial.println(WiFi.localIP());
            Serial.print("  RSSI: "); Serial.print(WiFi.RSSI()); Serial.println(" dBm");
        } else {
            Serial.println("  WiFi Status: Disconnected");
        }
    }
};
#endif // WIFI_MANAGER_H