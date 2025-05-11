#include "WiFiConfigManager.h"
#include "Config.h"

WiFiConfigManager* WiFiConfigManager::_instance = nullptr;

WiFiConfigManager::WiFiConfigManager()
    :
      _paramMqttAddr("mqtt",   "MQTT Server",      ConfigStore::getConfig().mqttServer.c_str(), 64),
      _paramMqttPort("port",   "MQTT Port",        ConfigStore::getConfig().mqttPort),
      _paramMqttTopic("topic", "MQTT Data Topic",  ConfigStore::getConfig().mqttDataTopic.c_str(), 64),
      _paramChipId("chip",     "Chip ID",          ConfigStore::getConfig().chipId.c_str(),       16),
      _paramMmpt("mmpt",       "mm per tip",       ConfigStore::getConfig().mmPerTip),
      _apLed(::Config::AP_LED_PIN)
{
    // Make this instance visible to the static callback
    _instance = this;
    _portal.setSaveConfigCallback(saveConfigCallback_);

    _portal.setAPCallback([this](WiFiManager* mgr) {
        _apLed.on();
    });
    _portal.setAPCallback([this](WiFiManager*) { _apLed.on(); });
    // register params
    _portal.addParameter(&_paramMqttAddr);
    _portal.addParameter(&_paramMqttPort);
    _portal.addParameter(&_paramMqttTopic);
    _portal.addParameter(&_paramChipId);
    _portal.addParameter(&_paramMmpt);
}

bool WiFiConfigManager::begin_(const bool forceAP) {
    ConfigStore::readFs();
    WiFiClass::mode(WIFI_STA);
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

    AppConfig cfg = ConfigStore::getConfig();
    cfg.mqttServer    = _paramMqttAddr.getValue();
    cfg.mqttPort      = _paramMqttPort.getValue();
    cfg.mqttDataTopic = _paramMqttTopic.getValue();
    cfg.chipId        = _paramChipId.getValue();
    cfg.mmPerTip      = _paramMmpt.getValue();
    cfg.wifiSSID      = WiFi.SSID();
    cfg.wifiPass      = WiFi.psk();
    ConfigStore::setConfig(cfg);

    if (_shouldSave) {
        Serial.println("Saving updated config…");
        ConfigStore::save();
    }
    return true;
}

void WiFiConfigManager::saveConfigCallback_() {
    Serial.println("Save callback was triggered, saveConfigCallback_");
    if (_instance) {
        Serial.println("Setting shouldSave to true");
        _instance->_shouldSave = true;
    }
}