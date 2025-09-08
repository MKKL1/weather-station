namespace IoTEventWorker.Models;

public class Histogram<T>(T[] tips, int slotCount, ushort slotSecs, DateTime startTime)
{
    public T[] Tips { get; } = tips;
    public int SlotCount { get; } = slotCount;
    public ushort SlotSecs { get; } = slotSecs;
    public DateTime StartTime { get; } = startTime;
}