using Worker;
using Worker.Models;
using Xunit;

namespace Tests;

public class HistogramAggregatorTest
{
    private readonly HistogramAggregator _aggregator = new();
    private readonly DateOnly _date = new DateOnly(2025, 8, 14);
    
    private DateTimeOffset CreateForConstDay(int hour, int minute, int second)
    {
        return new DateTimeOffset(_date, new TimeOnly(hour, minute, second), TimeSpan.Zero);
    }
    
    [Fact]
    public void ResampleHistogram_WhenBucketsAligned_ReturnCorrectBuckets()
    {
        var histogram = new Histogram<int>([1, 0, 1, 2, 1, 0], 6, 120, 
            CreateForConstDay(15, 18, 0));
        const float mmPerTip = 5f;
        const int target = 240; //4 minutes
        
        var actual = _aggregator.ResampleHistogram(histogram, mmPerTip, target);

        var expected = new Dictionary<DateTimeOffset, float>
        {
            [CreateForConstDay(15, 16, 0)] = 5f,
            [CreateForConstDay(15, 20, 0)] = 5f,
            [CreateForConstDay(15, 24, 0)] = 15f
        };

        Assert.Equal(expected, actual);
    }
    
    [Fact]
    public void ResampleHistogram_WhenBucketsNotAligned_ReturnCorrectBuckets()
    {
        var histogram = new Histogram<int>([1, 0, 1, 4, 1, 0], 6, 120, 
            CreateForConstDay(15, 18, 0));
        const float mmPerTip = 5f;
        const int target = 300; //5 minutes
        
        var actual = _aggregator.ResampleHistogram(histogram, mmPerTip, target);

        var expected = new Dictionary<DateTimeOffset, float>
        {
            [CreateForConstDay(15, 16, 0)] = 5f,
            [CreateForConstDay(15, 20, 0)] = 15f,
            [CreateForConstDay(15, 24, 0)] = 15f
        };

        Assert.Equal(expected, actual);
    }
    
    
}