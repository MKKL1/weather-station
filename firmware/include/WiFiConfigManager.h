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

class WiFiConfigManager {
public:
    const char* getMqttServer() const { return _paramMqttAddr.getValue(); }
    uint16_t getMqttPort() const { return _paramMqttPort.getValue(); } // Assuming PortParameter has matching getValue()
    const char* getMqttTopic() const { return _paramMqttTopic.getValue(); }
    const char* getChipId() const { return _paramChipId.getValue(); }
    float getMmPerTip() const { return _paramMmpt.getValue(); }
    /**
     * Gets the singleton instance of WiFiConfigManager.
     * If the instance hasn't been initialized yet, this will create it.
     */
    static WiFiConfigManager& getInstance();

    /**
     * Loads or reloads configuration from the filesystem.
     * Called automatically when needed, but can be called manually to refresh.
     */
    void loadConfig();

    /**
     * Connects to WiFi using saved credentials, or runs a captive portal if needed.
     * Must call init() before using this method.
     * Returns true if successfully connected to WiFi.
     */
    bool connect();

    /**
     * Forces the device into AP mode for configuration.
     * Must call init() before using this method.
     * Returns true if the configuration was successful.
     */
    bool forceAP();

    /**
     * Checks if the configuration has been initialized.
     */
    bool isInitialized() const { return _initialized; }

private:
    // Private constructor to prevent direct instantiation
    WiFiConfigManager();

    // Delete copy constructor and assignment operator
    WiFiConfigManager(const WiFiConfigManager&) = delete;
    WiFiConfigManager& operator=(const WiFiConfigManager&) = delete;

    bool beginConnection(bool forceAP);
    static void saveConfigCallback();

    WiFiManager           _portal;
    WiFiManagerParameter  _paramMqttAddr;
    PortParameter         _paramMqttPort;
    WiFiManagerParameter  _paramMqttTopic;
    WiFiManagerParameter  _paramChipId;
    FloatParameter        _paramMmpt;
    StatusLED             _apLed;
    bool                  _shouldSave = false;
    bool                  _initialized = false;

    // The singleton instance
    static WiFiConfigManager* _instance;
};

#endif // WIFI_CONFIG_MANAGER_H
