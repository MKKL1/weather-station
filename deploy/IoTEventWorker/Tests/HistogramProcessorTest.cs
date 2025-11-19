// using System.Reflection;
// using Worker;
// using Worker.Models;
// using Worker.Services;
// using Xunit;
//
// namespace Tests;
//
// public class HistogramProcessorTest
// {
//     private readonly HistogramProcessor _processor = new();
//     private readonly DateOnly _date = new DateOnly(2025, 8, 14);
//     
//     private DateTimeOffset CreateForConstDay(int hour, int minute, int second)
//     {
//         return new DateTimeOffset(_date, new TimeOnly(hour, minute, second), TimeSpan.Zero);
//     }
//     
//     [Fact]
//     public void ResampleHistogram_WhenBucketsAligned_ReturnCorrectBuckets()
//     {
//         var histogram = new Histogram<int>([1, 0, 1, 2, 1, 0], 120, 
//             CreateForConstDay(15, 18, 0));
//         const float mmPerTip = 5f;
//         const int target = 240; //4 minutes
//         
//         var actual = _processor.ResampleHistogram(histogram, mmPerTip, target);
//
//         var expected = new Dictionary<DateTimeOffset, float>
//         {
//             [CreateForConstDay(15, 16, 0)] = 5f,
//             [CreateForConstDay(15, 20, 0)] = 5f,
//             [CreateForConstDay(15, 24, 0)] = 15f
//         };
//
//         Assert.Equal(expected, actual);
//     }
//     
//     [Fact]
//     public void ResampleHistogram_WhenBucketsNotAligned_ReturnCorrectBuckets()
//     {
//         var histogram = new Histogram<int>([1, 0, 1, 4, 1, 0], 120, 
//             CreateForConstDay(15, 18, 0));
//         const float mmPerTip = 5f;
//         const int target = 300; //5 minutes
//         
//         var actual = _processor.ResampleHistogram(histogram, mmPerTip, target);
//
//         var expected = new Dictionary<DateTimeOffset, float>
//         {
//             [CreateForConstDay(15, 15, 0)] = 5f,
//             [CreateForConstDay(15, 20, 0)] = 15f,
//             [CreateForConstDay(15, 25, 0)] = 15f
//         };
//
//         Assert.Equal(expected, actual);
//     }
//     
//     [Fact]
//     public void ResampleHistogram_WhenEmptyBuckets_ReturnEmptyDictionary()
//     {
//         var histogram = new Histogram<int>([0,0,0,0,0,0], 120, 
//             CreateForConstDay(15, 18, 0));
//         const float mmPerTip = 5f;
//         const int target = 300; //5 minutes
//         
//         var actual = _processor.ResampleHistogram(histogram, mmPerTip, target);
//
//         var expected = new Dictionary<DateTimeOffset, float>();
//
//         Assert.Equal(expected, actual);
//     }
//     
//     [Fact]
//     public void ResampleHistogram_WhenSlotSecsGreaterThanTarget_ThrowException()
//     {
//         var histogram = new Histogram<int>([1,1,0,5,0,0], 120, 
//             CreateForConstDay(15, 18, 0));
//         const float mmPerTip = 5f;
//         const int target = 100; //Less than 120
//         
//         Assert.Throws<ArgumentException>(() => _processor.ResampleHistogram(histogram, mmPerTip, target));
//     }
//     
//     //I am not sure if checking different types is good for unit testing
//     //It also uses generics which may not be the best idea
//     [Theory]
//     [InlineData(typeof(byte))]
//     [InlineData(typeof(ushort))]
//     public void ResampleHistogram_ForDifferentHistogramTypes_ReturnCorrectBuckets(Type t)
//     {
//         var ints = new[] { 1, 0, 1, 2, 1, 0 };
//         var arr = Array.CreateInstance(t, ints.Length);
//         for (int i = 0; i < ints.Length; i++)
//         {
//             arr.SetValue(Convert.ChangeType(ints[i], t), i);
//         }
//         
//         var histogramType = typeof(Histogram<>).MakeGenericType(t);
//         var histogram = Activator.CreateInstance(
//             histogramType, arr, 120, CreateForConstDay(15, 18, 0));
//
//         const float mmPerTip = 5f;
//         const int target = 240;
//         
//         var method = _processor.GetType()
//             .GetMethods(BindingFlags.Instance | BindingFlags.Public)
//             .First(m => m.Name == "ResampleHistogram" && m.IsGenericMethodDefinition);
//
//         var generic = method.MakeGenericMethod(t);
//         var actualObj = generic.Invoke(_processor, [histogram, mmPerTip, target]);
//
//         var actual = (IDictionary<DateTimeOffset, float>)actualObj;
//
//         var expected = new Dictionary<DateTimeOffset, float>
//         {
//             [CreateForConstDay(15,16,0)] = 5f,
//             [CreateForConstDay(15,20,0)] = 5f,
//             [CreateForConstDay(15,24,0)] = 15f
//         };
//
//         Assert.Equal(expected, actual);
//     }
//     
//     [Fact]
//     public void ResampleHistogram_WhenHistogramPassesMultipleHours_ReturnCorrectBuckets()
//     {
//         var ints = new int[32];
//         ints[0] = 1;
//         ints[8] = 2;
//         ints[12] = 1;
//         ints[30] = 2;
//         
//         var histogram = new Histogram<int>(ints, 160, 
//             CreateForConstDay(15, 45, 4));
//         const float mmPerTip = 5f;
//         const int target = 300; //5 minutes
//         
//         var actual = _processor.ResampleHistogram(histogram, mmPerTip, target);
//
//         var expected = new Dictionary<DateTimeOffset, float>
//         {
//             [CreateForConstDay(15,45,0)] = 5f,
//             [CreateForConstDay(16,05,0)] = 10f,
//             [CreateForConstDay(16,15,0)] = 5f,
//             [CreateForConstDay(17,05,0)] = 10f,
//         };
//         Assert.Equal(expected, actual);
//     }
//     
//     [Fact]
//     public void AddToHistogram_WhenRainfallTimeBeforeHistogramStart_ThenSkipEntry()
//     {
//         var startTime = CreateForConstDay(15, 10, 0);
//         var hist = new Histogram<float>(new float[3], 60, startTime);
//         var rainfallBuckets = new Dictionary<DateTimeOffset, float>
//         {
//             [CreateForConstDay(15, 9, 0)] = 5.0f, //Before start
//             [CreateForConstDay(15, 10, 0)] = 4.0f,
//             [CreateForConstDay(15, 11, 0)] = 3.0f
//         };
//         
//         _processor.AddToHistogram(hist, rainfallBuckets);
//         
//         Assert.Equal(4.0f, hist.Data[0]);
//         Assert.Equal(3.0f, hist.Data[1]);
//     }
//     
//     [Fact]
//     public void AddToHistogram_WhenRainfallTimeAfterHistogramEnd_ThenSkipEntry()
//     {
//         // Arrange
//         var startTime = CreateForConstDay(15, 10, 0);
//         var hist = new Histogram<float>(new float[3], 60, startTime);
//         var rainfallBuckets = new Dictionary<DateTimeOffset, float>
//         {
//             [CreateForConstDay(15, 10, 30)] = 3.0f,
//             [CreateForConstDay(15, 13, 30)] = 5.0f // Beyond histogram end
//         };
//         
//         _processor.AddToHistogram(hist, rainfallBuckets);
//         
//         Assert.Equal([3.0f, 0f, 0f], hist.Data);
//     }
//     
//     [Fact]
//     public void AddToHistogram_WhenBucketsFit_ReturnCorrectHistogram()
//     {
//         var startTime = CreateForConstDay(15, 10, 0);
//         var hist = new Histogram<float>(new float[3], 60, startTime);
//         var rainfallBuckets = new Dictionary<DateTimeOffset, float>
//         {
//             [CreateForConstDay(15, 10, 30)] = 2.0f,
//             [CreateForConstDay(15, 11, 15)] = 4.0f,
//             [CreateForConstDay(15, 12, 45)] = 1.0f
//         };
//         
//         _processor.AddToHistogram(hist, rainfallBuckets);
//
//         Assert.Equal([2.0f, 4.0f, 1.0f], hist.Data);
//     }
//     
//     [Fact]
//     public void AddToHistogram_WhenMultipleRainfallInSameSlot_ThenUseMaximum()
//     {
//         var startTime = CreateForConstDay(15, 10, 0);
//         var hist = new Histogram<float>(new float[2], 60, startTime);
//         var rainfallBuckets = new Dictionary<DateTimeOffset, float>
//         {
//             [CreateForConstDay(15, 10, 10)] = 2.0f,
//             [CreateForConstDay(15, 10, 30)] = 5.0f,
//             [CreateForConstDay(15, 10, 50)] = 3.0f
//         };
//         
//         _processor.AddToHistogram(hist, rainfallBuckets);
//         
//         Assert.Equal([5f, 0f], hist.Data);
//     }
//     
//     [Fact]
//     public void AddToHistogram_WhenHistogramAlreadyHasValues_ThenUseMaximumWithExisting()
//     {
//         var startTime = CreateForConstDay(15, 10, 0);
//         var hist = new Histogram<float>([3.0f, 1.0f], 60, startTime);
//         var rainfallBuckets = new Dictionary<DateTimeOffset, float>
//         {
//             [CreateForConstDay(15, 10, 30)] = 2.0f,
//             [CreateForConstDay(15, 11, 30)] = 4.0f
//         };
//         
//         _processor.AddToHistogram(hist, rainfallBuckets);
//         
//         Assert.Equal([3f, 4f], hist.Data);
//     }
//
//     [Fact]
//     public void GetUniqueHours_WhenRepeatingHours_ReturnUnique()
//     {
//         var rainfallBuckets = new Dictionary<DateTimeOffset, float>
//         {
//             [CreateForConstDay(15, 10, 30)] = 2.0f,
//             [CreateForConstDay(15, 15, 30)] = 4.0f,
//             [CreateForConstDay(16, 25, 30)] = 4.0f,
//             [CreateForConstDay(17, 11, 30)] = 4.0f,
//             [CreateForConstDay(17, 31, 30)] = 4.0f,
//             [CreateForConstDay(16, 15, 30)] = 4.0f,
//             [CreateForConstDay(15, 21, 30)] = 4.0f,
//             [CreateForConstDay(16, 15, 30)] = 4.0f,
//             [CreateForConstDay(16, 21, 30)] = 4.0f,
//             [CreateForConstDay(16, 14, 30)] = 4.0f,
//         };
//
//         var actual = _processor.GetUniqueHours(rainfallBuckets);
//         var t = CreateForConstDay(15, 10, 30);
//         Assert.Equal([new DateTimeOffset(t.Year, t.Month, t.Day, 15, 0, 0, t.Offset), 
//             new DateTimeOffset(t.Year, t.Month, t.Day, 16, 0, 0, t.Offset),
//             new DateTimeOffset(t.Year, t.Month, t.Day, 17, 0, 0, t.Offset)], actual);
//     }
// }