using Moq;
using Worker;
using Worker.Infrastructure.Documents;
using Worker.Mappers;
using Worker.Models;
using Worker.Repositories;
using Worker.Services;
using Xunit;

namespace Tests.Services;

public class WeatherAggregationServiceTest
{
    private readonly Mock<IViewRepository> _viewRepository;
    private readonly Mock<IViewIdService> _viewIdService;
    private readonly Mock<IHistogramConverter> _histogramConverter;
    private readonly Mock<IHistogramProcessor> _histogramAggregator;
    private readonly WeatherAggregationService _service;

    public WeatherAggregationServiceTest()
    {
        _viewRepository = new Mock<IViewRepository>();
        _viewIdService = new Mock<IViewIdService>();
        _histogramConverter = new Mock<IHistogramConverter>();
        _histogramAggregator = new Mock<IHistogramProcessor>();
        _service = new WeatherAggregationService(_viewRepository.Object, 
            _viewIdService.Object, 
            _histogramConverter.Object,
            _histogramAggregator.Object);
    }

    [Fact]
    public async Task SaveLatestState_CallsAllDependenciesCorrectly()
    {
        var document = new RawEventDocument
        {
            id = "raw-123",
            DeviceId = "device-42",
            EventTimestamp = DateTimeOffset.Now,
            Payload = new RawEventDocument.PayloadBody
            {
                Temperature = 12.34f,
                Humidity = 56.7f,
                Pressure = 1012.3f,
                Rain = new RawEventDocument.Histogram(),
                RainfallMMPerTip = 0.25f
            }
        };

        var viewId = new Id("view-id");
        var dateId = new DateId("latest");
        var histogram = new Histogram<byte>(new byte[4], 150, DateTimeOffset.Now);
        var resampledData = new Dictionary<DateTimeOffset, float>();

        _viewIdService.Setup(s => s.GenerateIdLatest(document.DeviceId)).Returns(viewId);
        _viewIdService.Setup(s => s.GenerateDateIdLatest()).Returns(dateId);
        _histogramConverter.Setup(c => c.ToHistogramModel(document.Payload.Rain)).Returns(histogram);
        _histogramAggregator.Setup(a => a.ResampleHistogram(histogram, 0.25f, 300)).Returns(resampledData);
        
        
        await _service.SaveLatestState(document);
        
        
        _viewIdService.Verify(s => s.GenerateIdLatest("device-42"), Times.Once);
        _viewIdService.Verify(s => s.GenerateDateIdLatest(), Times.Once);
        _histogramConverter.Verify(c => c.ToHistogramModel(document.Payload.Rain), Times.Once);
        _histogramAggregator.Verify(a => a.ResampleHistogram(histogram, 0.25f, 300), Times.Once);
        _histogramAggregator.Verify(a => a.AddToHistogram(It.IsAny<Histogram<float>>(), resampledData), Times.Once);
        _viewRepository.Verify(r => r.UpdateLatestView(It.IsAny<AggregateModel<LatestStatePayload>>()), Times.Once);
    }

    [Fact]
    public async Task SaveLatestState_CreatesCorrectLatestStatePayload()
    {
        var document = new RawEventDocument
        {
            id = "raw-123",
            DeviceId = "device-42",
            EventTimestamp = DateTimeOffset.Now,
            Payload = new RawEventDocument.PayloadBody
            {
                Temperature = 25.5f,
                Humidity = 80.0f,
                Pressure = 1020.0f,
                Rain = new RawEventDocument.Histogram(),
                RainfallMMPerTip = 0.5f
            }
        };

        _viewIdService.Setup(s => s.GenerateIdLatest(It.IsAny<string>())).Returns(new Id("view-id"));
        _viewIdService.Setup(s => s.GenerateDateIdLatest()).Returns(new DateId("latest"));
        _histogramConverter.Setup(c => c.ToHistogramModel(It.IsAny<RawEventDocument.Histogram>()))
            .Returns(new Histogram<byte>(new byte[4], 150, DateTimeOffset.Now));
        _histogramAggregator.Setup(a => a.ResampleHistogram(It.IsAny<Histogram<byte>>(), It.IsAny<float>(), It.IsAny<int>()))
            .Returns(new Dictionary<DateTimeOffset, float>());

        AggregateModel<LatestStatePayload>? savedModel = null;
        _viewRepository.Setup(r => r.UpdateLatestView(It.IsAny<AggregateModel<LatestStatePayload>>()))
            .Callback<AggregateModel<LatestStatePayload>>(m => savedModel = m);


        await _service.SaveLatestState(document);


        Assert.NotNull(savedModel);
        Assert.Equal(document.DeviceId, savedModel.DeviceId.Value);
        Assert.Equal(document.EventTimestamp, savedModel.Payload.LastEventTs);
        Assert.Equal(document.id, savedModel.Payload.LastRawId);
        Assert.Equal(25.5f, savedModel.Payload.Temperature);
        Assert.Equal(80.0f, savedModel.Payload.Humidity);
        Assert.Equal(1020.0f, savedModel.Payload.Pressure);
    }

    [Fact]
    public async Task UpdateHourlyAggregate_CallsAllDependenciesCorrectly()
    {
        var document = new RawEventDocument
        {
            DeviceId = "device-42",
            EventTimestamp = DateTimeOffset.Now,
            Payload = new RawEventDocument.PayloadBody { RainfallMMPerTip = 0.25f, Rain = new RawEventDocument.Histogram() }
        };

        var histogram = new Histogram<byte>(new byte[4], 150, DateTimeOffset.Now);
        var resampledData = new Dictionary<DateTimeOffset, float>();
        var uniqueHours = new HashSet<DateTimeOffset> { DateTimeOffset.Now };

        _histogramConverter.Setup(c => c.ToHistogramModel(document.Payload.Rain)).Returns(histogram);
        _histogramAggregator.Setup(a => a.ResampleHistogram(histogram, 0.25f, 300)).Returns(resampledData);
        _histogramAggregator.Setup(a => a.GetUniqueHours(resampledData)).Returns(uniqueHours);
        _viewIdService.Setup(s => s.GenerateId(It.IsAny<string>(), It.IsAny<DateTimeOffset>(), DocType.Hourly)).Returns(new Id("hourly-id"));
        _viewIdService.Setup(s => s.GenerateDateId(It.IsAny<DateTimeOffset>(), DocType.Hourly)).Returns(new DateId("H2025-01-01T10"));
        _viewRepository.Setup(r => r.GetHourlyAggregate(It.IsAny<Id>(), It.IsAny<DeviceId>()))
            .ReturnsAsync((AggregateModel<HourlyAggregatePayload>?)null);


        await _service.UpdateHourlyAggregate(document);


        _histogramConverter.Verify(c => c.ToHistogramModel(document.Payload.Rain), Times.Once);
        _histogramAggregator.Verify(a => a.ResampleHistogram(histogram, 0.25f, 300), Times.Once);
        _histogramAggregator.Verify(a => a.GetUniqueHours(resampledData), Times.Once);
        _viewRepository.Verify(r => r.UpdateHourlyView(It.IsAny<AggregateModel<HourlyAggregatePayload>>()), Times.Once);
    }

    [Fact]
    public async Task UpdateHourlyAggregate_WithExistingAggregate_IncrementsMetrics()
    {
        var document = new RawEventDocument
        {
            DeviceId = "device-42",
            EventTimestamp = DateTimeOffset.Now,
            Payload = new RawEventDocument.PayloadBody 
            { 
                Temperature = 20.0f,
                Humidity = 60.0f,
                Pressure = 1015.0f,
                RainfallMMPerTip = 0.25f,
                Rain = new RawEventDocument.Histogram()
            }
        };

        var existingAggregate = new AggregateModel<HourlyAggregatePayload>
        {
            Id = new Id("hourly-id"),
            DeviceId = new DeviceId("device-42"),
            DateId = new DateId("H2025-01-01T10"),
            DocType = DocType.Hourly,
            Payload = new HourlyAggregatePayload
            {
                Temperature = new MetricAggregate(10.0f), // sum=10, count=1
                Humidity = new MetricAggregate(50.0f),
                Pressure = new MetricAggregate(1000.0f)
            }
        };

        _histogramConverter.Setup(c => c.ToHistogramModel(It.IsAny<RawEventDocument.Histogram>()))
            .Returns(new Histogram<byte>(new byte[4], 150, DateTimeOffset.Now));
        _histogramAggregator.Setup(a => a.ResampleHistogram(It.IsAny<Histogram<byte>>(), It.IsAny<float>(), It.IsAny<int>()))
            .Returns(new Dictionary<DateTimeOffset, float>());
        _histogramAggregator.Setup(a => a.GetUniqueHours(It.IsAny<Dictionary<DateTimeOffset, float>>()))
            .Returns(new HashSet<DateTimeOffset> { DateTimeOffset.Now });
        _viewIdService.Setup(s => s.GenerateId(It.IsAny<string>(), It.IsAny<DateTimeOffset>(), DocType.Hourly)).Returns(new Id("hourly-id"));
        _viewIdService.Setup(s => s.GenerateDateId(It.IsAny<DateTimeOffset>(), DocType.Hourly)).Returns(new DateId("H2025-01-01T10"));
        _viewRepository.Setup(r => r.GetHourlyAggregate(It.IsAny<Id>(), It.IsAny<DeviceId>())).ReturnsAsync(existingAggregate);

        AggregateModel<HourlyAggregatePayload>? savedModel = null;
        _viewRepository.Setup(r => r.UpdateHourlyView(It.IsAny<AggregateModel<HourlyAggregatePayload>>()))
            .Callback<AggregateModel<HourlyAggregatePayload>>(m => savedModel = m);


        await _service.UpdateHourlyAggregate(document);


        Assert.NotNull(savedModel);
        Assert.Equal(30.0f, savedModel.Payload.Temperature.Sum); // 10 + 20
        Assert.Equal(2, savedModel.Payload.Temperature.Count);
        Assert.Equal(110.0f, savedModel.Payload.Humidity.Sum); // 50 + 60
        Assert.Equal(2015.0f, savedModel.Payload.Pressure.Sum); // 1000 + 1015
    }

    [Fact]
    public async Task UpdateHourlyAggregate_WithMultipleHours_UpdatesAllHours()
    {
        var document = new RawEventDocument
        {
            DeviceId = "device-42",
            Payload = new RawEventDocument.PayloadBody { RainfallMMPerTip = 0.25f, Rain = new RawEventDocument.Histogram() }
        };

        var hour1 = new DateTimeOffset(2025, 1, 1, 10, 0, 0, TimeSpan.Zero);
        var hour2 = new DateTimeOffset(2025, 1, 1, 11, 0, 0, TimeSpan.Zero);

        _histogramConverter.Setup(c => c.ToHistogramModel(It.IsAny<RawEventDocument.Histogram>()))
            .Returns(new Histogram<byte>(new byte[4], 150, DateTimeOffset.Now));
        _histogramAggregator.Setup(a => a.ResampleHistogram(It.IsAny<Histogram<byte>>(), It.IsAny<float>(), It.IsAny<int>()))
            .Returns(new Dictionary<DateTimeOffset, float>());
        _histogramAggregator.Setup(a => a.GetUniqueHours(It.IsAny<Dictionary<DateTimeOffset, float>>()))
            .Returns(new HashSet<DateTimeOffset> { hour1, hour2 });
        _viewIdService.Setup(s => s.GenerateId(It.IsAny<string>(), It.IsAny<DateTimeOffset>(), DocType.Hourly)).Returns(new Id("hourly-id"));
        _viewIdService.Setup(s => s.GenerateDateId(It.IsAny<DateTimeOffset>(), DocType.Hourly)).Returns(new DateId("H2025-01-01T10"));
        _viewRepository.Setup(r => r.GetHourlyAggregate(It.IsAny<Id>(), It.IsAny<DeviceId>()))
            .ReturnsAsync((AggregateModel<HourlyAggregatePayload>?)null);


        await _service.UpdateHourlyAggregate(document);


        _viewRepository.Verify(r => r.UpdateHourlyView(It.IsAny<AggregateModel<HourlyAggregatePayload>>()), Times.Exactly(2));
        _histogramAggregator.Verify(a => a.AddToHistogram(It.IsAny<Histogram<float>>(), It.IsAny<Dictionary<DateTimeOffset, float>>()), Times.Exactly(2));
    }
}