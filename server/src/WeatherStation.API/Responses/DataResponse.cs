using WeatherStation.Domain.Entities;
using WeatherStation.Application.Enums;

namespace WeatherStation.API.Responses;

public class DataResponse
{
    public string DeviceId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeInterval Interval { get; set; }
    public IEnumerable<Measurement?> Measurements { get; set; } //TODO: don't use Measurement, simplify this data type 

    public DataResponse(string deviceId, DateTime startTime, DateTime endTime, TimeInterval interval, IEnumerable<Measurement?> measurements)
    {
        DeviceId = deviceId;
        StartTime = startTime;
        EndTime = endTime;
        Interval = interval;
        Measurements = measurements;
    }
}