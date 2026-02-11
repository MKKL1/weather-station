using WeatherStation.Core.Dto;

namespace WeatherStation.Core;

public interface IDataPointMapper<in T>
{
    //TODO could also populate existing dictionary and data point list
    IDictionary<MetricTypes,IEnumerable<DataPoint>> Map(T entity, IEnumerable<MetricTypes> metric);
}