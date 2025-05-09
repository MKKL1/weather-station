#ifndef WIFI_CONFIG_MANAGER_H
#define WIFI_CONFIG_MANAGER_H

#include <FS.h>
#include <WiFiManager.h>
#include <controllers/StatusLED.h>

/**
 * Handles captive-portal configuration of:
 *   - WiFi SSID / password (via WiFiManager)
 *   - MQTT server address
 *   - Rain gauge mm-per-tip calibration
 *
 * Stores settings in LITTLEFS (/wifi_config.json).
 */
class WiFiConfigManager {
public:
    struct dConfig {
        String wifiSSID;
        String wifiPass;
        String mqttServer;
        String mmPerTip;    // stored as string, convert to float with toFloat()
    };

    WiFiConfigManager();
    /**
     * Mounts LITTLEFS, loads any existing config, then
     * runs a captive portal if needed.
     * Returns true if we ended up connected to WiFi.
     */
    bool begin();

    bool forceAP();

    [[nodiscard]] dConfig getConfig() const { return _cfg; }

private:
    void loadConfig_();
    void saveConfig_() const;
    bool begin_(bool forceAP);
    static void saveConfigCallback_();

    WiFiManager           _portal;
    WiFiManagerParameter  _paramMqtt;
    WiFiManagerParameter  _paramMmpt;
    dConfig                _cfg;
    bool                  _shouldSave = false;
    StatusLED             _apLed;

    // Single-instance pointer for static callback
    static WiFiConfigManager* _instance;
};

#endif // WIFI_CONFIG_MANAGER_H
