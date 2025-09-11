
using Worker.Services;
using Xunit;

namespace Tests.Services;

public class ViewIdServiceTest
{
    private readonly ViewIdService _service = new();
    private const string DeviceId = "Device123";
    
    [Fact]
    public void GenerateHourly_WithValidDeviceIdAndTimestamp_ReturnsExpectedFormat()
    {
        var timestamp = new DateTimeOffset(2025, 09, 11, 15, 45, 39, TimeSpan.Zero);
        
        var result = _service.GenerateHourly(DeviceId, timestamp);
        
        Assert.Equal("Device123|hourly|2025-09-11T15", result);
    }
    
    [Theory]
    [InlineData(2025, 9, 11, 15, 45, 39, 2.0, "Device123|hourly|2025-09-11T13")] // +02 -> UTC 13
    [InlineData(2025, 9, 11, 15, 45, 39, -5.0, "Device123|hourly|2025-09-11T20")] // -05 -> UTC 20
    [InlineData(2025, 9, 11, 15, 45, 39, 5.5, "Device123|hourly|2025-09-11T10")] // +05:30 -> UTC 10
    [InlineData(2025, 9, 11, 0, 15, 0, 2.0, "Device123|hourly|2025-09-10T22")]  // 00:15 +02 -> UTC 22:15 previous day
    public void GenerateHourly_UTCNormalized_ReturnsExpectedTime(int y, int mo, int d, int h, int mi, int s, double offsetHours, string expected)
    {
        var offset = TimeSpan.FromHours(offsetHours);
        var ts = new DateTimeOffset(y, mo, d, h, mi, s, offset);

        var actual = _service.GenerateHourly(DeviceId, ts);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GenerateDaily_WithValidDeviceIdAndTimestamp_ReturnsExpectedFormat()
    {
        var timestamp = new DateTimeOffset(2025, 09, 11, 15, 45, 39, TimeSpan.Zero);

        var result = _service.GenerateDaily(DeviceId, timestamp);

        Assert.Equal("Device123|daily|2025-09-11", result);
    }

    [Theory]
    [InlineData(2025, 9, 11, 15, 45, 39, 2.0, "Device123|daily|2025-09-11")] // +02
    [InlineData(2025, 9, 11, 15, 45, 39, -5.0, "Device123|daily|2025-09-11")] // -05
    [InlineData(2025, 9, 11, 15, 45, 39, 5.5, "Device123|daily|2025-09-11")] // +05:30
    [InlineData(2025, 9, 11, 0, 15, 0, 2.0, "Device123|daily|2025-09-10")]  // prev day
    [InlineData(2025, 1, 1, 0, 30, 0, 2.0, "Device123|daily|2024-12-31")]  // year wrap
    [InlineData(2024, 2, 29, 12, 0, 0, 0.0, "Device123|daily|2024-02-29")] // leap day
    public void GenerateDaily_UTCNormalized_ReturnsExpectedTime(int y, int mo, int d, int h, int mi, int s, double offsetHours, string expected)
    {
        var offset = TimeSpan.FromHours(offsetHours);
        var ts = new DateTimeOffset(y, mo, d, h, mi, s, offset);

        var actual = _service.GenerateDaily(DeviceId, ts);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GenerateMonthly_WithValidDeviceIdAndTimestamp_ReturnsExpectedFormat()
    {
        var timestamp = new DateTimeOffset(2025, 09, 11, 15, 45, 39, TimeSpan.Zero);

        var result = _service.GenerateMonthly(DeviceId, timestamp);

        Assert.Equal("Device123|monthly|2025-09", result);
    }

    [Theory]
    [InlineData(2025, 9, 11, 15, 45, 39, 2.0, "Device123|monthly|2025-09")]
    [InlineData(2025, 10, 1, 0, 30, 0, 2.0, "Device123|monthly|2025-09")] // prev month
    [InlineData(2025, 12, 31, 23, 30, 0, -2.0, "Device123|monthly|2026-01")] // next month
    [InlineData(2024, 3, 1, 0, 30, 0, 2.0, "Device123|monthly|2024-02")] // prev month (leap)
    [InlineData(2025, 9, 11, 15, 45, 39, -5.0, "Device123|monthly|2025-09")]  // -05
    public void GenerateMonthly_UTCNormalized_ReturnsExpectedTime(int y, int mo, int d, int h, int mi, int s, double offsetHours, string expected)
    {
        var offset = TimeSpan.FromHours(offsetHours);
        var ts = new DateTimeOffset(y, mo, d, h, mi, s, offset);

        var actual = _service.GenerateMonthly(DeviceId, ts);

        Assert.Equal(expected, actual);
    }

}