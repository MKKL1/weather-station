using Moq;
using Worker;
using Worker.Documents;
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
    private readonly Mock<IHistogramAggregator> _histogramAggregator;
    private readonly WeatherAggregationService _service;

    public WeatherAggregationServiceTest()
    {
        _viewRepository = new Mock<IViewRepository>();
        _viewIdService = new Mock<IViewIdService>();
        _histogramConverter = new Mock<IHistogramConverter>();
        _histogramAggregator = new Mock<IHistogramAggregator>();
        _service = new WeatherAggregationService(_viewRepository.Object, 
            _viewIdService.Object, 
            _histogramConverter.Object,
            _histogramAggregator.Object);
    }

    [Fact]
    public async Task SaveLatestState_WhenValidRawEvent_ShouldSaveLatestView()
    {
        var document = new RawEventDocument
        {
            id = "raw-123",
            DeviceId = "device-42",
            EventType = "raw",
            EventTimestamp = new DateTimeOffset(2025, 1, 1, 1, 1, 1, TimeSpan.Zero),
            Payload = new RawEventDocument.PayloadBody
            {
                Temperature = 12.34f,
                Humidity = 56.7f,
                Pressure = 1012.3f,
                Rain = new RawEventDocument.Histogram(),
                RainfallMMPerTip = 0.25f
            }
        };
        var id = "fakeid";
        _viewIdService.Setup(r => r.GenerateLatest(document.DeviceId)).Returns(id);

        var histogram = new Histogram<byte>(new byte[16], 16, 150, 
            new DateTimeOffset(2025, 1, 1, 1, 1, 1, TimeSpan.Zero));
        _histogramConverter.Setup(s => s.ToHistogramModel(It.IsAny<RawEventDocument.Histogram>()))
            .Returns(histogram);

        var resampled  = new Dictionary<DateTimeOffset, float>();
        _histogramAggregator.Setup(s =>
                s.ResampleHistogram(It.IsAny<Histogram<byte>>(), It.IsAny<float>(), It.IsAny<int>()))
            .Returns(resampled );
        
        AggregateModel<LatestStatePayload>? capturedModel = null;
        _viewRepository.Setup(r => r.UpdateLatestView(It.IsAny<AggregateModel<LatestStatePayload>>()))
            .Callback<AggregateModel<LatestStatePayload>>(m => capturedModel = m)
            .Returns(Task.CompletedTask);
        
        //Act
        await _service.SaveLatestState(document);
        
        _histogramAggregator.Verify(s => s.AddToHistogram(It.IsAny<Histogram<float>>(), resampled ), Times.Once);
        _histogramAggregator.Verify(s => s.ResampleHistogram(It.IsAny<Histogram<byte>>(), document.Payload.RainfallMMPerTip, It.IsAny<int>()), Times.Once);
        _viewRepository.Verify(r => r.UpdateLatestView(It.IsAny<AggregateModel<LatestStatePayload>>()), Times.Once);
        
        Assert.NotNull(capturedModel);
        Assert.Equal(id, capturedModel.Id);
        Assert.Equal(document.DeviceId, capturedModel.DeviceId);
        Assert.Equal(document.EventTimestamp, capturedModel.Payload.LastEventTs);
        Assert.Equal(document.id, capturedModel.Payload.LastRawId);
        Assert.Equal(document.Payload.Temperature, capturedModel.Payload.Temperature);
        
        Assert.NotNull(capturedModel.Payload.Rain);
    }

    [Fact]
    public async Task UpdateHourlyAggregate_WhenNewDocument_ShouldSaveHourlyViews()
    {
        var d1 = new DateTimeOffset(2025, 1, 1, 1, 0, 0, TimeSpan.Zero);
        var d2 = new DateTimeOffset(2025, 1, 1, 2, 0, 0, TimeSpan.Zero);
        var document = new RawEventDocument
        {
            id = "raw-123",
            DeviceId = "device-42",
            EventType = "raw",
            EventTimestamp = new DateTimeOffset(2025, 1, 1, 1, 1, 1, TimeSpan.Zero),
            Payload = new RawEventDocument.PayloadBody
            {
                Temperature = 12.34f,
                Humidity = 56.7f,
                Pressure = 1012.3f,
                Rain = new RawEventDocument.Histogram(),
                RainfallMMPerTip = 0.25f
            }
        };
        var id1 = "fakeid1";
        var id2 = "fakeid2";
        
        var histogram = new Histogram<byte>(new byte[16], 16, 150, 
            new DateTimeOffset(2025, 1, 1, 1, 1, 1, TimeSpan.Zero));
        _histogramConverter.Setup(s => s.ToHistogramModel(It.IsAny<RawEventDocument.Histogram>()))
            .Returns(histogram);

        var resampled  = new Dictionary<DateTimeOffset, float>();
        _histogramAggregator.Setup(s =>
                s.ResampleHistogram(It.IsAny<Histogram<byte>>(), It.IsAny<float>(), It.IsAny<int>()))
            .Returns(resampled );
        
        _viewIdService.Setup(r => r.GenerateHourly(document.DeviceId, d1)).Returns(id1);
        _viewIdService.Setup(r => r.GenerateHourly(document.DeviceId, d2)).Returns(id2);

        _histogramAggregator.Setup(s => s.GetUniqueHours(resampled))
            .Returns([d1,d2]);

        //Return null, so that new document has to be created
        _viewRepository.Setup(s => s.GetHourlyAggregate(id1, document.DeviceId))
            .ReturnsAsync((AggregateModel<HourlyAggregatePayload>?)null);
        
        await _service.UpdateHourlyAggregate(document);
        
        //TODO check if metrics like Temperature, Pressure... were set properly
        
        _viewRepository.Verify(s => s.GetHourlyAggregate(id1, document.DeviceId), Times.Once);
        _viewRepository.Verify(s => s.GetHourlyAggregate(id2, document.DeviceId), Times.Once);
        _histogramAggregator.Verify(s => 
            s.AddToHistogram(It.IsAny<Histogram<float>>(), It.IsAny<Dictionary<DateTimeOffset,float>>()), Times.Exactly(2));
        
        _viewRepository.Verify(s => s.UpdateHourlyView(It.IsAny<AggregateModel<HourlyAggregatePayload>>()), Times.Exactly(2));
    }
}