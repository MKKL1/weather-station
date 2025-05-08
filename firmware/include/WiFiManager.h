// WiFiManager.h
#ifndef WIFI_MANAGER_H
#define WIFI_MANAGER_H

#include <WiFi.h>
#include <Arduino.h>

namespace WiFiManager {

    /**
     * Connects to the specified WiFi network within the given timeout.
     * Returns true if connected successfully.
     */
    inline bool connect(const char* ssid, const char* password, uint32_t timeoutMs = 10000) {
        if (WiFi.status() == WL_CONNECTED) {
            Serial.println("WiFi already connected.");
            return true;
        }

        WiFi.mode(WIFI_STA);
        WiFi.begin(ssid, password);
        Serial.printf("Connecting to WiFi '%s'", ssid);

        unsigned long start = millis();
        while (WiFi.status() != WL_CONNECTED && millis() - start < timeoutMs) {
            Serial.print('.');
            delay(500);
        }

        if (WiFi.status() == WL_CONNECTED) {
            Serial.println("\nWiFi connected.");
            return true;
        }

        Serial.println("\nWiFi connection failed.");
        WiFi.disconnect(true);
        WiFi.mode(WIFI_OFF);
        return false;
    }

    /**
     * Disconnects from WiFi and powers off the radio.
     */
    inline void disconnect() {
        if (WiFi.status() == WL_CONNECTED) {
            Serial.println("Disconnecting WiFi.");
            WiFi.disconnect(true);
            WiFi.mode(WIFI_OFF);
        }
    }

    /**
     * Returns true if WiFi is currently connected.
     */
    inline bool isConnected() {
        return WiFi.status() == WL_CONNECTED;
    }

    /**
     * Prints the current IP and RSSI if connected, or status if not.
     */
    inline void printStatus() {
        if (WiFi.status() == WL_CONNECTED) {
            Serial.print("IP: ");
            Serial.println(WiFi.localIP());
            Serial.print("RSSI: ");
            Serial.print(WiFi.RSSI());
            Serial.println(" dBm");
        } else {
            Serial.println("WiFi: Disconnected");
        }
    }

} // namespace WiFiManager

#endif // WIFI_MANAGER_H