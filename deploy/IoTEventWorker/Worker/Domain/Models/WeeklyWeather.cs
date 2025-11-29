using Worker.Domain.Entities;
using Worker.Domain.ValueObjects;

namespace Worker.Domain.Models;

public class WeeklyWeather
{
    public string DeviceId { get; }
    public int Year { get; }
    public int WeekNumber { get; }
    public string? Version { get; set; }

    public StatSummary?[] DailyTemperatures { get; private set; } = new StatSummary?[7];
    public StatSummary?[] DailyHumidities { get; private set; } = new StatSummary?[7];
    public StatSummary?[] DailyPressures { get; private set; } = new StatSummary?[7];
    public StatSummary?[] DailyRainfall { get; private set; } = new StatSummary?[7];

    public StatSummary? Temperature { get; private set; }
    public StatSummary? Humidity { get; private set; }
    public StatSummary? Pressure { get; private set; }
    public StatSummary? Rain { get; private set; }

    public WeeklyWeather(string deviceId, int year, int weekNumber)
    {
        DeviceId = deviceId;
        Year = year;
        WeekNumber = weekNumber;
    }

    public void ApplyDay(DailyWeather day)
    {
        if (day.DeviceId != DeviceId) throw new ArgumentException("Device ID mismatch");
        if (!day.IsFinalized) throw new ArgumentException("Day must be finalized");

        int index = GetIsoDayOfWeekIndex(day.DayTimestamp);

        if (day.Temperature != null)
            DailyTemperatures[index] = new StatSummary(
                day.Temperature.Min, day.Temperature.Max, day.Temperature.GetAverage);

        if (day.Humidity != null)
            DailyHumidities[index] = new StatSummary(
                day.Humidity.Min, day.Humidity.Max, day.Humidity.GetAverage);

        if (day.Pressure != null)
            DailyPressures[index] = new StatSummary(
                day.Pressure.Min, day.Pressure.Max, day.Pressure.GetAverage);

        if (day.Rain != null && day.Rain.Histogram.Data.Count > 0)
        {
            var values = day.Rain.Histogram.Data.Values;
            DailyRainfall[index] = new StatSummary(
                Min: values.Min(),
                Max: values.Max(),
                Avg: values.Average()
            );
        }
        else
        {
            DailyRainfall[index] = new StatSummary(0, 0, 0);
        }

        RecalculateWeeklyStats();
    }

    private void RecalculateWeeklyStats()
    {
        Temperature = CalculateStats(DailyTemperatures);
        Humidity = CalculateStats(DailyHumidities);
        Pressure = CalculateStats(DailyPressures);
        Rain = CalculateStats(DailyRainfall);
    }

    private static StatSummary? CalculateStats(StatSummary?[] buckets)
    {
        var validDays = buckets.Where(b => b.HasValue).Select(b => b!.Value).ToList();

        if (validDays.Count == 0) return null;

        return new StatSummary(
            Min: validDays.Min(x => x.Min),
            Max: validDays.Max(x => x.Max),
            Avg: validDays.Average(x => x.Avg)
        );
    }

    private static int GetIsoDayOfWeekIndex(DateTimeOffset date)
    {
        var day = date.DayOfWeek;
        return day == DayOfWeek.Sunday ? 6 : (int)day - 1;
    }
}