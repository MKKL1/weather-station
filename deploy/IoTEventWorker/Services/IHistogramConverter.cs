using IoTEventWorker.Domain.Models;
using IoTEventWorker.Models;
using weatherstation.eventhandler.Entities;

namespace IoTEventWorker.Domain.Services;

//TODO move somewhere else, that's not a business service
public interface IHistogramConverter
{
    public Histogram<byte> ToHistogramModel(RawEventDocument.Histogram histogramDocument);
}