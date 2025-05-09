#include "WiFiConfigManager.h"
#include <LittleFS.h>
#include <ArduinoJson.h>
#include "Config.h"

static constexpr char CONFIG_FILE[] = "/wifi_config.json";
WiFiConfigManager* WiFiConfigManager::_instance = nullptr;

WiFiConfigManager::WiFiConfigManager()
  : _paramMqtt("mqtt", "MQTT Server", "mqtt.example.com", 64),
    _paramMmpt("mmpt", "mm per tip", "2.25", 8),
    _apLed(Config::AP_LED_PIN)
{
    // Make this instance visible to the static callback
    _instance = this;
    _portal.setSaveConfigCallback(saveConfigCallback_);

    _portal.setAPCallback([this](WiFiManager* mgr) {
        _apLed.on();
    });

    // Register parameters before portal
    _portal.addParameter(&_paramMqtt);
    _portal.addParameter(&_paramMmpt);
}

bool WiFiConfigManager::begin() {
    return begin_(false);
}

bool WiFiConfigManager::forceAP() {
    return begin_(true);
}

bool WiFiConfigManager::begin_(const bool forceAP) {
    // Attempt to mount LITTLEFS
    if (!LittleFS.begin(/*formatOnFail=*/true)) {
        Serial.println("LITTLEFS mount (with auto-format) failed");
        return false;
    }
    // If mounted, load any existing config
    loadConfig_();

    // 2) Station mode only
    WiFiClass::mode(WIFI_STA);

    // 3) Start portal
    constexpr auto AP_SSID = "WeatherStationAP";
    constexpr auto AP_PASS = "configureme";

    _shouldSave = false;
    _portal.setBreakAfterConfig(true);
    bool ok;
    if (forceAP) {
        Serial.println("forceAP started");
        ok = _portal.startConfigPortal(AP_SSID, AP_PASS);
    } else {
        Serial.println("autoconnect started");
        ok = _portal.autoConnect(AP_SSID, AP_PASS);
    }
    _apLed.off();
    if (!ok) {
        Serial.println("Portal failed or timed out");
        // return false;
    }
    Serial.println("Exited portal, continue normally");

    // 4) Read back parameters
    _cfg.mqttServer = _paramMqtt.getValue();
    _cfg.mmPerTip   = _paramMmpt.getValue();
    _cfg.wifiSSID   = WiFi.SSID();
    _cfg.wifiPass   = WiFi.psk();

    // 5) Save if callback triggered
    if (_shouldSave) {
        Serial.println("Save callback was triggered, saving configuration...");
        saveConfig_();
    } else {
        Serial.println("Save callback was NOT triggered (e.g., portal timed out or exited without saving). Configuration not saved.");
    }

    Serial.print("Mqtt server: ");
    Serial.println(_cfg.mqttServer);
    return true;
}

void WiFiConfigManager::loadConfig_() {
    if (!LittleFS.exists(CONFIG_FILE)) {
        Serial.println("loadConfig_(): Config file does not exist.");
        return;
    }
    File file = LittleFS.open(CONFIG_FILE, "r");
    if (!file) return;

    JsonDocument doc;
    DeserializationError err = deserializeJson(doc, file);
    file.close();
    if (err) {
        Serial.println("Failed to parse config file");
        return;
    }

    // Populate existing config
    _cfg.wifiSSID   = doc["wifiSSID"].as<String>();
    _cfg.wifiPass   = doc["wifiPass"].as<String>();
    _cfg.mqttServer = doc["mqttServer"].as<String>();
    _cfg.mmPerTip   = doc["mmPerTip"].as<String>();

    // Override initial parameter defaults so portal fields pre-populate
    _paramMqtt.setValue(_cfg.mqttServer.c_str(), _cfg.mqttServer.length());
    _paramMmpt.setValue(_cfg.mmPerTip.c_str(),     _cfg.mmPerTip.length());
}

void WiFiConfigManager::saveConfig_() const {
    JsonDocument doc;
    doc["wifiSSID"]   = _cfg.wifiSSID;
    doc["wifiPass"]   = _cfg.wifiPass;
    doc["mqttServer"] = _cfg.mqttServer;
    doc["mmPerTip"]   = _cfg.mmPerTip;

    File file = LittleFS.open(CONFIG_FILE, FILE_WRITE);
    if (!file) {
        Serial.println("Failed to open config for writing");
        return;
    }
    serializeJsonPretty(doc, file);
    file.close();
    Serial.print("Config saved to ");
    Serial.println(CONFIG_FILE);
}

void WiFiConfigManager::saveConfigCallback_() {
    Serial.println("Save callback was triggered, saveConfigCallback_");
    if (_instance) {
        Serial.println("Setting shouldSave to true");
        _instance->_shouldSave = true;
    }
}
