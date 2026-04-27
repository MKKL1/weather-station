using System.Globalization;
using System.Net;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Worker.Domain;
using Worker.Domain.Models;

namespace Worker.Services;

public class WeeklyAggregationService(
    ILogger<WeeklyAggregationService> logger,
    IWeatherRepository repository)
{
    /**
     * Find weekly documents corresponding to days in finalizedDays and update weekly documents.
     *
     * Example: finalizedDays holds 5 days (Friday, Saturday, Sunday, Monday, Tuesday); 3 (Fri,Sat,Sun) in week#1, 2 (Mon, Tue) in week#2.
     * week#1 and week#2 are fetched, updated locally and then persisted.
     */
    public async Task SyncDaysToWeeksAsync(IEnumerable<DailyWeather> finalizedDays)
    {
        var daysList = finalizedDays.ToList();
        if (daysList.Count == 0) return;

        var groups = daysList
            .GroupBy(d => {
                var (year, week) = GetIsoWeek(d.DayTimestamp);
                return (d.DeviceId, Year: year, Week: week);
            })
            .ToList();

        var tasks = groups.Select(ProcessSingleWeekGroupAsync);
        
        await Task.WhenAll(tasks);
    }

    private async Task ProcessSingleWeekGroupAsync(
        IGrouping<(string DeviceId, int Year, int Week), DailyWeather> group)
    {
        var (deviceId, year, weekNum) = group.Key;
        int attempts = 0;
        const int MaxRetries = 5;

        while (attempts < MaxRetries)
        {
            attempts++;
            try
            {
                var weekly = await repository.GetWeekly(deviceId, year, weekNum);

                if (weekly == null)
                {
                    weekly = new WeeklyWeather(deviceId, year, weekNum);
                }

                foreach (var day in group)
                {
                    weekly.ApplyDay(day);
                }

                await repository.SaveWeekly(weekly);
                return; 
            }
            catch (CosmosException ex) when (
                ex.StatusCode == HttpStatusCode.PreconditionFailed || // 412 (Update Conflict)
                ex.StatusCode == HttpStatusCode.Conflict)             // 409 (Insert Conflict)
            {
                logger.LogWarning("Concurrency conflict for {DevId} W{Week}. Retrying (Attempt {N})", 
                    deviceId, weekNum, attempts);
                
                await Task.Delay(TimeSpan.FromMilliseconds(new Random().Next(10, 100)));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Fatal error processing week for {DevId}", deviceId);
                throw; 
            }
        }

        throw new Exception($"Failed to update week for {deviceId} after {MaxRetries} attempts.");
    }

    private static (int Year, int Week) GetIsoWeek(DateTimeOffset date)
    {
        return (ISOWeek.GetYear(date.DateTime), ISOWeek.GetWeekOfYear(date.DateTime));
    }
}