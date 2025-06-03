#ifndef APPCONFIG_H
#define APPCONFIG_H

#include <Arduino.h>
#include <LittleFS.h>

#include <utility>

#include "JsonDataFormatter.h"

#define MQTT_SERVER_MAX_LEN 64
#define MQTT_USER_MAX_LEN   32
#define MQTT_PASS_MAX_LEN   64
#define MQTT_TOPIC_MAX_LEN  64
#define WIFI_SSID_MAX_LEN   32
#define WIFI_PASS_MAX_LEN   64
#define CHIP_ID_MAX_LEN     16

struct AppConfig {
  char     wifiSSID[WIFI_SSID_MAX_LEN];
  char     wifiPass[WIFI_PASS_MAX_LEN];
  char     mqttServer[MQTT_SERVER_MAX_LEN];
  uint16_t mqttPort;
  char     mqttUser[MQTT_USER_MAX_LEN];
  char     mqttPassword[MQTT_PASS_MAX_LEN];
  char     mqttDataTopic[MQTT_TOPIC_MAX_LEN];
  char     chipId[CHIP_ID_MAX_LEN];
  float    mmPerTip;
};

namespace ConfigStore {

  static auto CONFIG_FILE = "/wifi_config.json";

  RTC_DATA_ATTR static AppConfig cfg = {
    "MySSID",
    "MyPass",
    "mqtt.example.com",
    1883,
    "MyMqttUser",
    "MyMqttPass",
    "weather/data",
    "ESP32-001",
    2.25f
  };
  static boolean loaded = false;


  inline bool readFs() {
    if (!LittleFS.begin(true)) {
      Serial.println("Failed to mount LittleFS");
      return false;
    }
    if (!LittleFS.exists(CONFIG_FILE)) {
      Serial.printf("Config file %s not found.\n", CONFIG_FILE);
      return false;
    }

    File f = LittleFS.open(CONFIG_FILE, "r");
    if (!f) {
      Serial.println("Failed to open config file for reading.");
      return false;
    }

    JsonDocument doc;
    DeserializationError error = deserializeJson(doc, f);
    f.close();

    if (error) {
      Serial.print("deserializeJson() failed: ");
      Serial.println(error.c_str());
      return false;
    }

    strlcpy(cfg.wifiSSID, doc["wifiSSID"] | cfg.wifiSSID, WIFI_SSID_MAX_LEN);
    strlcpy(cfg.wifiPass, doc["wifiPass"] | cfg.wifiPass, WIFI_PASS_MAX_LEN);
    strlcpy(cfg.mqttServer, doc["mqttServer"] | cfg.mqttServer, MQTT_SERVER_MAX_LEN);
    cfg.mqttPort = doc["mqttPort"] | cfg.mqttPort;
    strlcpy(cfg.mqttPassword, doc["mqttPass"] | cfg.mqttPassword, MQTT_PASS_MAX_LEN);
    strlcpy(cfg.mqttUser, doc["mqttUser"] | cfg.mqttUser, MQTT_USER_MAX_LEN);
    strlcpy(cfg.mqttDataTopic, doc["mqttDataTopic"] | cfg.mqttDataTopic, MQTT_TOPIC_MAX_LEN);
    strlcpy(cfg.chipId, doc["chipId"] | cfg.chipId, CHIP_ID_MAX_LEN);
    cfg.mmPerTip = doc["mmPerTip"] | cfg.mmPerTip;

    loaded = true;
    Serial.printf("Read config mqtt %s\n", cfg.mqttServer);
    Serial.printf("Read cfg mqtt %s\n", cfg.mqttServer);
    return true;
  }

  /// Write current cfg to flash
  inline void save() {
    if (!LittleFS.begin(true)) {
      Serial.println("Failed to mount LittleFS for saving");
      return;
    }
    File f = LittleFS.open(CONFIG_FILE, FILE_WRITE);
    if (!f) {
      Serial.println("Failed to open config file for writing.");
      return;
    }
    JsonDocument doc;
    doc["wifiSSID"]     = cfg.wifiSSID;
    doc["wifiPass"]     = cfg.wifiPass;
    doc["mqttServer"]   = cfg.mqttServer;
    doc["mqttPort"]     = cfg.mqttPort;
    doc["mqttUser"]     = cfg.mqttUser;
    doc["mqttPass"]     = cfg.mqttPassword;
    doc["mqttDataTopic"]= cfg.mqttDataTopic;
    doc["chipId"]       = cfg.chipId;
    doc["mmPerTip"]     = cfg.mmPerTip;

    if (serializeJsonPretty(doc, f) == 0) {
      Serial.println("Failed to write to config file");
    } else {
      Serial.printf("Saved config with MQTT: %s\n", cfg.mqttServer);
    }
    f.close();
  }

}



#endif //APPCONFIG_H
