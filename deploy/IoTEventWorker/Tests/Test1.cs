using IoTEventWorker;
using IoTEventWorker.Models;

namespace Tests;

[TestClass]
public sealed class Test1
{
    [TestMethod]
    public void TestMethod1()
    {
        var aggregator = new HistogramAggregator();
        var histogram = new Histogram<int>([0, 0, 0, 15, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0], 16, 120,
            new DateTime(2025, 8, 14, 15, 18, 0));
        aggregator.AggregateToHourlyBins(histogram, 2.5f, 300);
    }
}