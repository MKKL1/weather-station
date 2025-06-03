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
                    const char* chipId,
                    const char* mqttTopic,
                    const char* mqttUser,
                    const char* mqttPass,
                    float       mmpt,
                    uint32_t instanceId);

    void send(const WeatherEntry& currentEntry);

private:
    void connectServices();
    void retryQueued();
    void publishEntry(const WeatherEntry& entry);

    MQTT::Client _mqtt;
    bool         _mqttConnected = false;
    const char*  _chipId;
    const char*  _mqttTopic{};
    const float  _mmpt;
    const uint32_t _instanceId;
};

#endif // REPORT_SENDER_H