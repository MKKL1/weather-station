using IoTEventWorker.Documents;
using IoTEventWorker.Models;

namespace IoTEventWorker.Services;

//TODO move somewhere else, that's not a business service
public interface IHistogramConverter
{
    public Histogram<byte> ToHistogramModel(RawEventDocument.Histogram histogramDocument);
}