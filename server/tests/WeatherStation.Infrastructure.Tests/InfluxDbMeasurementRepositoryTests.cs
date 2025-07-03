using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InfluxDB.Client;
using InfluxDB.Client.Core.Flux.Domain;
using Microsoft.Extensions.Configuration;
using Moq;
using NodaTime;
using NodaTime.Extensions;
using WeatherStation.Domain.Entities;
using WeatherStation.Infrastructure;
using Xunit;

namespace WeatherStation.Infrastructure.Tests
{
    public class InfluxDbMeasurementRepositoryTests
    {
        private readonly Mock<IQueryApi> _queryApiMock;
        private readonly InfluxDbMeasurementRepository _repository;

        public InfluxDbMeasurementRepositoryTests()
        {
            const string bucket = "test-bucket";
            const string org = "test-org";
            
            _queryApiMock     = new Mock<IQueryApi>();
            var influxClientMock = new Mock<IInfluxDBClient>();
            influxClientMock
                .Setup(c => c.GetQueryApi(null))
                .Returns(_queryApiMock.Object);
            
            var clientFactoryMock = new Mock<IInfluxDbClientFactory>();
            clientFactoryMock
                .Setup(f => f.GetClient())
                .Returns(influxClientMock.Object);

            _repository = new InfluxDbMeasurementRepository(
                clientFactoryMock.Object,
                bucket,
                org
            );
        }

        [Fact]
        public async Task GetSnapshot_ReturnsNull_WhenNoData()
        {
            // Arrange
            _queryApiMock
                .Setup(q => q.QueryAsync(It.IsAny<string>(), It.IsAny<string>(), CancellationToken.None))
                .ReturnsAsync([]);

            // Act
            var result = await _repository.GetSnapshot("device-1");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetSnapshot_ReturnsMeasurement_WhenDataExists()
        {
            // Arrange: Create a FluxTable with one record
            var record = new FluxRecord(0);
            record.Values["device_id"] = "device-1";
            var timestamp = Instant.FromUtc(2025, 7, 1, 12, 0).ToDateTimeOffset().ToInstant();
            record.Values["_time"] = timestamp;
            record.Values["temperature"] = 22.5;
            record.Values["pressure"] = 1013.25;
            record.Values["humidity"] = 55.0;
            record.Values["avg_rainfall_mm"] = 1.2;

            var table = new FluxTable();
            table.Records.Add(record);

            _queryApiMock
                .Setup(q => q.QueryAsync(It.IsAny<string>(), It.IsAny<string>(), CancellationToken.None))
                .ReturnsAsync([table]);

            // Act
            var result = await _repository.GetSnapshot("device-1");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("device-1", result.DeviceId);
            Assert.Equal(timestamp, result.Timestamp.ToInstant());
            Assert.Equal(22.5f, result.Values[MetricType.Temperature]);
            Assert.Equal(1013.25f, result.Values[MetricType.Pressure]);
            Assert.Equal(55f, result.Values[MetricType.Humidity]);
            Assert.Equal(1.2f, result.Values[MetricType.Rainfall]);
        }

        [Fact]
        public async Task GetSnapshot_ThrowsInvalidOperationException_WhenTimeMissing()
        {
            // Arrange: Record without _time
            var record = new FluxRecord(0)
            {
                Values =
                {
                    ["device_id"] = "device-1",
                    ["temperature"] = 20.0
                }
            };

            var table = new FluxTable();
            table.Records.Add(record);

            _queryApiMock
                .Setup(q => q.QueryAsync(It.IsAny<string>(), It.IsAny<string>(), CancellationToken.None))
                .ReturnsAsync([table]);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _repository.GetSnapshot("device-1")
            );
        }
    }
}