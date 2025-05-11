#ifndef WIFI_CONFIG_MANAGER_H
#define WIFI_CONFIG_MANAGER_H

#include <AppConfig.h>
#include <FS.h>
#include <WiFiManager.h>
#include <controllers/StatusLED.h>

class PortParameter : public WiFiManagerParameter {
public:
    /// \param id           unique field ID
    /// \param placeholder  label/placeholder text
    /// \param value        default port (e.g. 1883)
    /// \param length       max input characters (5 digits + maybe one extra)
    PortParameter(const char *id,
                  const char *placeholder,
                  uint16_t value = 1883,
                  const uint8_t length = 6)
      : WiFiManagerParameter("")
    {
        // custom HTML ensures numeric input with bounds
        constexpr auto attrs = "type=\"number\" min=\"1\" max=\"65535\"";
        init(id,
             placeholder,
             String(value).c_str(),
             length,
             attrs,
             WFM_LABEL_BEFORE);
    }

    /// Returns the entered port, clamped to [1, 65535]
    [[nodiscard]] uint16_t getValue() const {
        const long v = String(WiFiManagerParameter::getValue()).toInt();
        if (v < 1)      return 1;
        if (v > 65535)  return 65535;
        return static_cast<uint16_t>(v);
    }
};


// class IntParameter : public WiFiManagerParameter {
// public:
//     IntParameter(const char *id, const char *placeholder, long value, const uint8_t length = 10)
//         : WiFiManagerParameter("") {
//         init(id, placeholder, String(value).c_str(), length, "", WFM_LABEL_BEFORE);
//     }
//
//     [[nodiscard]] long getValue() const {
//         return String(WiFiManagerParameter::getValue()).toInt();
//     }
// };

class FloatParameter : public WiFiManagerParameter {
public:
    FloatParameter(const char *id, const char *placeholder, const float value, const uint8_t length = 10)
        : WiFiManagerParameter("") {
        init(id, placeholder, String(value).c_str(), length, "", WFM_LABEL_BEFORE);
    }

    [[nodiscard]] float getValue() const {
        return String(WiFiManagerParameter::getValue()).toFloat();
    }
};

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
    WiFiConfigManager();
    /**
     * Mounts LITTLEFS, loads any existing config, then
     * runs a captive portal if needed.
     * Returns true if we ended up connected to WiFi.
     */
    bool begin() {
        return begin_(false);
    }

    bool forceAP() {
        return begin_(true);
    }

private:
    bool begin_(bool forceAP);
    static void saveConfigCallback_();

    WiFiManager           _portal;
    WiFiManagerParameter  _paramMqttAddr;
    PortParameter         _paramMqttPort;
    WiFiManagerParameter  _paramMqttTopic;
    WiFiManagerParameter  _paramChipId;
    FloatParameter        _paramMmpt;
    StatusLED             _apLed;
    bool                  _shouldSave = false;

    static WiFiConfigManager* _instance;
};

#endif // WIFI_CONFIG_MANAGER_H
