namespace IoTEventWorker.Domain.Models;

public class Histogram(byte[] tips, byte slotCount, ushort slotSecs, DateTime startTime)
{
    public byte[] Tips { get; } = tips;
    public byte SlotCount { get; } = slotCount;
    public ushort SlotSecs { get; } = slotSecs;
    public DateTime StartTime { get; } = startTime;
}