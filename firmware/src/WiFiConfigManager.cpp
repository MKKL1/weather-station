#include "WiFiConfigManager.h"
#include "Config.h"

// Initialize static member
WiFiConfigManager* WiFiConfigManager::_instance = nullptr;

WiFiConfigManager& WiFiConfigManager::getInstance() {
    if (_instance == nullptr) {
        _instance = new WiFiConfigManager();
    }
    return *_instance;
}

WiFiConfigManager::WiFiConfigManager()
    :
      _paramMqttAddr("mqtt",   "MQTT Server",      ConfigStore::cfg.mqttServer, MQTT_SERVER_MAX_LEN),
      _paramMqttPort("port",   "MQTT Port",        ConfigStore::cfg.mqttPort),
      _paramMqttTopic("topic", "MQTT Data Topic",  ConfigStore::cfg.mqttDataTopic, MQTT_TOPIC_MAX_LEN),
      _paramChipId("chip",     "Chip ID",          ConfigStore::cfg.chipId, CHIP_ID_MAX_LEN),
      _paramMmpt("mmpt",       "mm per tip",       ConfigStore::cfg.mmPerTip),
      _paramMqttPass("pass", "Mqtt Password", ConfigStore::cfg.mqttPassword, MQTT_PASS_MAX_LEN),
      _paramMqttUser("user", "Mqtt User", ConfigStore::cfg.mqttUser, MQTT_USER_MAX_LEN),
      _apLed(::Config::AP_LED_PIN)
{
    // Set up the portal configuration
    _portal.setSaveConfigCallback(saveConfigCallback);

    _portal.setAPCallback([this](WiFiManager*) {
        Serial.println("AP ready LED ON");
        _apLed.on();
    });

    // register params
    _portal.addParameter(&_paramMqttAddr);
    _portal.addParameter(&_paramMqttPort);
    _portal.addParameter(&_paramMqttUser);
    _portal.addParameter(&_paramMqttPass);
    _portal.addParameter(&_paramMqttTopic);
    _portal.addParameter(&_paramChipId);
    _portal.addParameter(&_paramMmpt);

    // Load configuration immediately
    loadConfig();
}

void WiFiConfigManager::loadConfig() {
    ConfigStore::readFs(); // Load data into ConfigStore::cfg first

    // Convert numeric types to strings for standard WiFiManagerParameter if needed
    // (Your PortParameter/FloatParameter might handle this internally)
    char portStr[8];
    snprintf(portStr, sizeof(portStr), "%d", ConfigStore::cfg.mqttPort);

    char mmptStr[10];
    // Use snprintf for float formatting safety
    snprintf(mmptStr, sizeof(mmptStr), "%.2f", ConfigStore::cfg.mmPerTip);

    // Update parameters with loaded values FROM ConfigStore::cfg
    _paramMqttAddr.setValue(ConfigStore::cfg.mqttServer, MQTT_SERVER_MAX_LEN);
    _paramMqttPort.setValue(portStr, sizeof(portStr));
    _paramMqttUser.setValue(ConfigStore::cfg.mqttUser, MQTT_USER_MAX_LEN);
    _paramMqttPass.setValue(ConfigStore::cfg.mqttPassword, MQTT_PASS_MAX_LEN);
    _paramMqttTopic.setValue(ConfigStore::cfg.mqttDataTopic, MQTT_TOPIC_MAX_LEN);
    _paramChipId.setValue(ConfigStore::cfg.chipId, CHIP_ID_MAX_LEN);
    _paramMmpt.setValue(mmptStr, sizeof(mmptStr));

    _initialized = true;
    Serial.println("WiFiConfigManager loaded config values from ConfigStore");
}

bool WiFiConfigManager::connect() {
    // Ensure config is loaded
    if (!_initialized) {
        loadConfig();
    }
    return beginConnection(false);
}

bool WiFiConfigManager::forceAP() {
    // Ensure config is loaded
    if (!_initialized) {
        loadConfig();
    }
    return beginConnection(true);
}

bool WiFiConfigManager::beginConnection(const bool forceAP) {
    Serial.printf("Mqtt begin connection: %s \n", ConfigStore::cfg.mqttServer);
    WiFiClass::mode(WIFI_STA);
    constexpr auto AP_SSID = "WeatherStationAP";
    constexpr auto AP_PASS = "configureme";
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
        //return false;
    }

    Serial.println("Exited portal, continue normally");

    if (_shouldSave) {
        _shouldSave = false;

        strlcpy(ConfigStore::cfg.mqttServer, _paramMqttAddr.getValue(), MQTT_SERVER_MAX_LEN);
        ConfigStore::cfg.mqttPort = _paramMqttPort.getValue();
        strlcpy(ConfigStore::cfg.mqttUser, _paramMqttUser.getValue(), MQTT_USER_MAX_LEN);
        strlcpy(ConfigStore::cfg.mqttPassword, _paramMqttPass.getValue(), MQTT_PASS_MAX_LEN);
        strlcpy(ConfigStore::cfg.mqttDataTopic, _paramMqttTopic.getValue(), MQTT_TOPIC_MAX_LEN);
        strlcpy(ConfigStore::cfg.chipId, _paramChipId.getValue(), CHIP_ID_MAX_LEN);
        ConfigStore::cfg.mmPerTip = _paramMmpt.getValue();

        strlcpy(ConfigStore::cfg.wifiSSID, WiFi.SSID().c_str(), WIFI_SSID_MAX_LEN);
        strlcpy(ConfigStore::cfg.wifiPass, WiFi.psk().c_str(), WIFI_PASS_MAX_LEN);

        ConfigStore::save();

        Serial.println("Config saved after portal exit");
        Serial.printf("Saved MQTT Server: %s\n", ConfigStore::cfg.mqttServer);
    }

    return true;
}

void WiFiConfigManager::saveConfigCallback() {
    Serial.println("Save callback was triggered");
    if (_instance) {
        Serial.println("Setting shouldSave to true");
        _instance->_shouldSave = true;

        Serial.printf("Portal MQTT Addr Param: %s\n", _instance->_paramMqttAddr.getValue());
    }
}