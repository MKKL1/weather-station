using IoTEventWorker.Domain.Models;
using weatherstation.eventhandler.Entities;

namespace IoTEventWorker.Domain.Services;

public interface IHistogramConverter
{
    public Histogram ToHistogramModel(RawEventDocument.Histogram histogramDocument);
}