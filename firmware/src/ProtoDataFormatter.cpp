//
// Created by krystian on 23.04.2025.
//

#include <Arduino.h>
#include "ProtoDataFormatter.h"

#include <Config.h>
#include <pb_encode.h>
#include "weather.pb.h"

bool encode_string(pb_ostream_t* stream, const pb_field_t* field, void* const* arg)
{
    const auto str = static_cast<const char *>(*arg);

    if (!pb_encode_tag_for_field(stream, field))
        return false;

    return pb_encode_string(stream, (uint8_t*)str, strlen(str));
}

struct HistogramDataArg {
    const uint8_t* ptr;
    size_t         len;
};

// This function will be called by nanopb when it needs the contents of `data`
static bool histogram_data_callback(pb_ostream_t *stream,
                                    const pb_field_t *field,
                                    void * const *arg)
{
    const auto* harg = static_cast<const HistogramDataArg*>(*arg);

    if (!pb_encode_tag_for_field(stream, field))
        return false;

    if (!pb_encode_varint(stream, harg->len))
        return false;

    return pb_write(stream, harg->ptr, harg->len);
}

size_t ProtoDataFormatter::formatData(const WeatherData &data, uint8_t *buffer, size_t bufferSize) const {
    if (!buffer || bufferSize == 0) return 0;

    pb_ostream_t stream = pb_ostream_from_buffer(
        buffer,
        bufferSize
    );

    proto_WeatherData weather_proto = proto_WeatherData_init_default;

    weather_proto.created_at = data.weatherEntry.timestamp_s;
    weather_proto.temperature   = data.weatherEntry.temperature;
    weather_proto.humidity      = data.weatherEntry.humidity;
    weather_proto.pressure      = data.weatherEntry.pressure;

    HistogramDataArg arg = {
        .ptr = data.weatherEntry.tip_bitmask,
        .len = WeatherEntry::BITMASK_SIZE_BYTES
    };

    proto_Histogram hist = proto_Histogram_init_default;
    hist.count             = WeatherEntry::NUM_INTERVALS_PER_ENTRY;
    hist.interval_duration = WeatherEntry::INTERVAL_DURATION_S;
    hist.start_time        = WeatherEntry::calcStartTime(data.weatherEntry.timestamp_s);
    hist.data.funcs.encode = &histogram_data_callback;
    hist.data.arg  = &arg;
    weather_proto.tips = hist;
    weather_proto.has_tips = true;

    proto_DeviceInfo dev_info = proto_DeviceInfo_init_default;
    dev_info.id.arg = reinterpret_cast<void*>(const_cast<char*>(Config::SENSOR_ID));
    dev_info.id.funcs.encode = &encode_string;
    dev_info.mmPerTip = Config::MM_PER_TIP;
    weather_proto.info = dev_info;
    weather_proto.has_info = true;


    // 4) Encode!
    if (!pb_encode(&stream, proto_WeatherData_fields, &weather_proto)) {
        Serial.print("Protobuf encode failed: ");
        Serial.println(PB_GET_ERROR(&stream));
        return 0;
    }

    // 5) stream.bytes_written now contains the number of bytes used
    return stream.bytes_written;
}