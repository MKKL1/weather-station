using Worker.Models;
using Worker.Services;
using Xunit;

namespace Tests.Services;

public class ViewIdServiceTest
{
    private readonly ViewIdService _service = new();
    private const string DeviceId = "Device123";
    
    [Fact]
    public void GenerateIdLatest_WithValidDeviceId_ReturnsExpectedFormat()
    {
        var result = _service.GenerateIdLatest(DeviceId);
        
        Assert.Equal("Device123|latest", result.Value);
    }
    
    [Fact]
    public void GenerateId_Hourly_WithValidDeviceIdAndTimestamp_ReturnsExpectedFormat()
    {
        var timestamp = new DateTimeOffset(2025, 09, 11, 15, 45, 39, TimeSpan.Zero);
        
        var result = _service.GenerateId(DeviceId, timestamp, DocType.Hourly);
        
        Assert.Equal("Device123|hourly|2025-09-11T15", result.Value);
    }
    
    [Theory]
    [InlineData(2025, 9, 11, 15, 45, 39, 2.0, "Device123|hourly|2025-09-11T13")] // +02 -> UTC 13
    [InlineData(2025, 9, 11, 15, 45, 39, -5.0, "Device123|hourly|2025-09-11T20")] // -05 -> UTC 20
    [InlineData(2025, 9, 11, 15, 45, 39, 5.5, "Device123|hourly|2025-09-11T10")] // +05:30 -> UTC 10
    [InlineData(2025, 9, 11, 0, 15, 0, 2.0, "Device123|hourly|2025-09-10T22")]  // 00:15 +02 -> UTC 22:15 previous day
    public void GenerateId_Hourly_UTCNormalized_ReturnsExpectedTime(int y, int mo, int d, int h, int mi, int s, double offsetHours, string expected)
    {
        var offset = TimeSpan.FromHours(offsetHours);
        var ts = new DateTimeOffset(y, mo, d, h, mi, s, offset);

        var actual = _service.GenerateId(DeviceId, ts, DocType.Hourly);

        Assert.Equal(expected, actual.Value);
    }

    [Fact]
    public void GenerateId_Daily_WithValidDeviceIdAndTimestamp_ReturnsExpectedFormat()
    {
        var timestamp = new DateTimeOffset(2025, 09, 11, 15, 45, 39, TimeSpan.Zero);

        var result = _service.GenerateId(DeviceId, timestamp, DocType.Daily);

        Assert.Equal("Device123|daily|2025-09-11", result.Value);
    }

    [Theory]
    [InlineData(2025, 9, 11, 15, 45, 39, 2.0, "Device123|daily|2025-09-11")] // +02
    [InlineData(2025, 9, 11, 15, 45, 39, -5.0, "Device123|daily|2025-09-11")] // -05
    [InlineData(2025, 9, 11, 15, 45, 39, 5.5, "Device123|daily|2025-09-11")] // +05:30
    [InlineData(2025, 9, 11, 0, 15, 0, 2.0, "Device123|daily|2025-09-10")]  // prev day
    [InlineData(2025, 1, 1, 0, 30, 0, 2.0, "Device123|daily|2024-12-31")]  // year wrap
    [InlineData(2024, 2, 29, 12, 0, 0, 0.0, "Device123|daily|2024-02-29")] // leap day
    public void GenerateId_Daily_UTCNormalized_ReturnsExpectedTime(int y, int mo, int d, int h, int mi, int s, double offsetHours, string expected)
    {
        var offset = TimeSpan.FromHours(offsetHours);
        var ts = new DateTimeOffset(y, mo, d, h, mi, s, offset);

        var actual = _service.GenerateId(DeviceId, ts, DocType.Daily);

        Assert.Equal(expected, actual.Value);
    }

    [Fact]
    public void GenerateId_Monthly_WithValidDeviceIdAndTimestamp_ReturnsExpectedFormat()
    {
        var timestamp = new DateTimeOffset(2025, 09, 11, 15, 45, 39, TimeSpan.Zero);

        var result = _service.GenerateId(DeviceId, timestamp, DocType.Monthly);

        Assert.Equal("Device123|monthly|2025-09", result.Value);
    }

    [Theory]
    [InlineData(2025, 9, 11, 15, 45, 39, 2.0, "Device123|monthly|2025-09")]
    [InlineData(2025, 10, 1, 0, 30, 0, 2.0, "Device123|monthly|2025-09")] // prev month
    [InlineData(2025, 12, 31, 23, 30, 0, -2.0, "Device123|monthly|2026-01")] // next month
    [InlineData(2024, 3, 1, 0, 30, 0, 2.0, "Device123|monthly|2024-02")] // prev month (leap)
    [InlineData(2025, 9, 11, 15, 45, 39, -5.0, "Device123|monthly|2025-09")]  // -05
    public void GenerateId_Monthly_UTCNormalized_ReturnsExpectedTime(int y, int mo, int d, int h, int mi, int s, double offsetHours, string expected)
    {
        var offset = TimeSpan.FromHours(offsetHours);
        var ts = new DateTimeOffset(y, mo, d, h, mi, s, offset);

        var actual = _service.GenerateId(DeviceId, ts, DocType.Monthly);

        Assert.Equal(expected, actual.Value);
    }

    [Fact]
    public void GenerateId_Latest_CallsGenerateIdLatest()
    {
        var result = _service.GenerateId(DeviceId, DateTimeOffset.Now, DocType.Latest);
        var expected = _service.GenerateIdLatest(DeviceId);
        
        Assert.Equal(expected.Value, result.Value);
    }

    [Fact]
    public void GenerateDateIdLatest_ReturnsExpectedValue()
    {
        var result = _service.GenerateDateIdLatest();
        
        Assert.Equal("latest", result.Value);
    }

    [Fact]
    public void GenerateDateId_Latest_CallsGenerateDateIdLatest()
    {
        var result = _service.GenerateDateId(DateTimeOffset.Now, DocType.Latest);
        var expected = _service.GenerateDateIdLatest();
        
        Assert.Equal(expected.Value, result.Value);
    }

    [Fact]
    public void GenerateDateId_Hourly_ReturnsExpectedFormat()
    {
        var timestamp = new DateTimeOffset(2025, 09, 11, 15, 45, 39, TimeSpan.Zero);
        
        var result = _service.GenerateDateId(timestamp, DocType.Hourly);
        
        Assert.Equal("H2025-09-11T15", result.Value);
    }

    [Fact]
    public void GenerateDateId_Daily_ReturnsExpectedFormat()
    {
        var timestamp = new DateTimeOffset(2025, 09, 11, 15, 45, 39, TimeSpan.Zero);
        
        var result = _service.GenerateDateId(timestamp, DocType.Daily);
        
        Assert.Equal("D2025-09-11", result.Value);
    }

    [Fact]
    public void GenerateDateId_Monthly_ReturnsExpectedFormat()
    {
        var timestamp = new DateTimeOffset(2025, 09, 11, 15, 45, 39, TimeSpan.Zero);
        
        var result = _service.GenerateDateId(timestamp, DocType.Monthly);
        
        Assert.Equal("M2025-09", result.Value);
    }

    [Theory]
    [InlineData(2025, 9, 11, 15, 45, 39, 2.0, "H2025-09-11T13")] // +02 -> UTC 13
    [InlineData(2025, 9, 11, 0, 15, 0, 2.0, "H2025-09-10T22")]  // 00:15 +02 -> UTC 22:15 previous day
    public void GenerateDateId_Hourly_UTCNormalized_ReturnsExpectedTime(int y, int mo, int d, int h, int mi, int s, double offsetHours, string expected)
    {
        var offset = TimeSpan.FromHours(offsetHours);
        var ts = new DateTimeOffset(y, mo, d, h, mi, s, offset);

        var actual = _service.GenerateDateId(ts, DocType.Hourly);

        Assert.Equal(expected, actual.Value);
    }

    [Theory]
    [InlineData(2025, 9, 11, 0, 15, 0, 2.0, "D2025-09-10")]  // prev day
    [InlineData(2025, 1, 1, 0, 30, 0, 2.0, "D2024-12-31")]  // year wrap
    public void GenerateDateId_Daily_UTCNormalized_ReturnsExpectedTime(int y, int mo, int d, int h, int mi, int s, double offsetHours, string expected)
    {
        var offset = TimeSpan.FromHours(offsetHours);
        var ts = new DateTimeOffset(y, mo, d, h, mi, s, offset);

        var actual = _service.GenerateDateId(ts, DocType.Daily);

        Assert.Equal(expected, actual.Value);
    }

    [Theory]
    [InlineData(2025, 10, 1, 0, 30, 0, 2.0, "M2025-09")] // prev month
    [InlineData(2025, 12, 31, 23, 30, 0, -2.0, "M2026-01")] // next month
    public void GenerateDateId_Monthly_UTCNormalized_ReturnsExpectedTime(int y, int mo, int d, int h, int mi, int s, double offsetHours, string expected)
    {
        var offset = TimeSpan.FromHours(offsetHours);
        var ts = new DateTimeOffset(y, mo, d, h, mi, s, offset);

        var actual = _service.GenerateDateId(ts, DocType.Monthly);

        Assert.Equal(expected, actual.Value);
    }
}