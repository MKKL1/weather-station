#ifndef APPCONFIG_H
#define APPCONFIG_H

#include <Arduino.h>
#include <LittleFS.h>

#include <utility>

#include "JsonDataFormatter.h"

#define MQTT_SERVER_MAX_LEN 64
#define MQTT_TOPIC_MAX_LEN  64
#define WIFI_SSID_MAX_LEN   32
#define WIFI_PASS_MAX_LEN   64
#define CHIP_ID_MAX_LEN     16

struct AppConfig {
  char     wifiSSID[WIFI_SSID_MAX_LEN];
  char     wifiPass[WIFI_PASS_MAX_LEN];
  char     mqttServer[MQTT_SERVER_MAX_LEN];
  uint16_t mqttPort;
  char     mqttDataTopic[MQTT_TOPIC_MAX_LEN];
  char     chipId[CHIP_ID_MAX_LEN];
  float    mmPerTip;
};

namespace ConfigStore {

  // filename in LittleFS
  static auto CONFIG_FILE = "/wifi_config.json";

  // Stored in RTC slow memory to survive deep sleep.
  // Initialized with defaults.
  RTC_DATA_ATTR static AppConfig cfg = {
    /* wifiSSID     */ "MySSID",
    /* wifiPass     */ "MyPass",
    /* mqttServer   */ "mqtt.example.com",
    /* mqttPort     */ 1883,
    /* mqttDataTopic*/ "weather/data",
    /* chipId       */ "ESP32-001",
    /* mmPerTip     */ 2.25f
  };
  static boolean loaded = false;


  inline bool readFs() {
    if (!LittleFS.begin(true)) { // Ensure LittleFS is mounted
      Serial.println("Failed to mount LittleFS");
      return false;
    }
    if (!LittleFS.exists(CONFIG_FILE)) {
      Serial.printf("Config file %s not found.\n", CONFIG_FILE);
      return false; // Keep default values if file doesn't exist
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

    // Use strlcpy for safe copying into fixed-size buffers
    strlcpy(cfg.wifiSSID, doc["wifiSSID"] | cfg.wifiSSID, WIFI_SSID_MAX_LEN); // Use default if key missing
    strlcpy(cfg.wifiPass, doc["wifiPass"] | cfg.wifiPass, WIFI_PASS_MAX_LEN);
    strlcpy(cfg.mqttServer, doc["mqttServer"] | cfg.mqttServer, MQTT_SERVER_MAX_LEN);
    cfg.mqttPort = doc["mqttPort"] | cfg.mqttPort;
    strlcpy(cfg.mqttDataTopic, doc["mqttDataTopic"] | cfg.mqttDataTopic, MQTT_TOPIC_MAX_LEN);
    strlcpy(cfg.chipId, doc["chipId"] | cfg.chipId, CHIP_ID_MAX_LEN);
    cfg.mmPerTip = doc["mmPerTip"] | cfg.mmPerTip;

    loaded = true; // Set flag indicating successful load

    Serial.printf("Read config mqtt %s\n", cfg.mqttServer); // No .c_str() needed for char[]
    Serial.printf("Read cfg mqtt %s\n", cfg.mqttServer);
    return true;
  }

  /// Write current cfg to flash
  inline void save() {
    // Ensure LittleFS is mounted before writing
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
