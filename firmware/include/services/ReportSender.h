#ifndef REPORT_SENDER_H
#define REPORT_SENDER_H

#include "MqttClientWrapper.h"
#include "WeatherEntry.h"
#include "TimeManager.h"
#include "WiFiManager.h"

class ReportSender {
public:
    ReportSender(const char* mqttServer,
                 uint16_t    mqttPort,
                 const char* mqttClientIdPrefix);

    void send(const WeatherEntry& currentEntry);

private:
    void connectServices();
    void retryQueued();
    void publishEntry(const WeatherEntry& entry);

    MQTT::Client _mqtt;
    bool         _mqttConnected = false;
};

#endif // REPORT_SENDER_H