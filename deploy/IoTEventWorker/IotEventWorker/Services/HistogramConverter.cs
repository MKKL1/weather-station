using IoTEventWorker.Documents;
using IoTEventWorker.Models;

namespace IoTEventWorker.Services;

//TODO move to model mapper but for proto
public class HistogramConverter: IHistogramConverter
{
    public Histogram<byte> ToHistogramModel(RawEventDocument.Histogram histogramDocument)
    {
        int slotCount = histogramDocument.SlotCount;
        var tips = DecodeTipsFromBase64(histogramDocument.Data, slotCount);
        return new Histogram<byte>(tips, histogramDocument.SlotCount, histogramDocument.SlotSecs, histogramDocument.StartTime);
    }

    private static byte[] DecodeTipsFromBase64(string base64Data, int slotCount)
    {
        var result = new byte[Math.Max(0, slotCount)];
        if (slotCount <= 0) return result;
        if (string.IsNullOrEmpty(base64Data)) return result;

        byte[] packed;
        try
        {
            packed = Convert.FromBase64String(base64Data);
        }
        catch (FormatException ex)
        {
            throw new ArgumentException("Histogram.data is not valid base64.", nameof(base64Data), ex);
        }
        
        int neededBytes = (slotCount + 1) / 2;
        if (packed.Length < neededBytes)
        {
            var tmp = new byte[neededBytes];
            Array.Copy(packed, tmp, packed.Length);
            packed = tmp;
        }

        for (int i = 0; i < slotCount; i++)
        {
            int byteIndex = i / 2;
            bool isHighNibble = (i % 2) == 1;
            byte value = packed[byteIndex];

            byte count = (byte)(isHighNibble ? ((value >> 4) & 0x0F) : (value & 0x0F));
            result[i] = count;
        }

        return result;
    }
}