// using Worker.Infrastructure.Documents;
// using Worker.Mappers;
// using Worker.Services;
// using Xunit;
//
// namespace Tests.Services;
//
// public class HistogramConverterTest
// {
//     [Fact]
//     public void ToHistogramModel_WhenValidHistogramDocument_ReturnsModel()
//     {
//         const string base64 = "IUM=";
//
//         var doc = new RawEventDocument.Histogram
//         {
//             SlotCount = 4,
//             Data = base64,
//             SlotSecs = 5,
//             StartTime = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc)
//         };
//
//         var converter = new HistogramConverter();
//         var model = converter.ToHistogramModel(doc);
//
//         Assert.NotNull(model);
//         Assert.Equal(4, model.SlotCount);
//         Assert.Equal(5, model.IntervalSeconds);
//         Assert.Equal(doc.StartTime, model.StartTime);
//         
//         Assert.Equal(new byte[] { 1, 2, 3, 4 }, model.Data);
//     }
// }