using Worker.Documents;
using Worker.Models;

namespace Worker.Services;

//TODO move somewhere else, that's not a business service
public interface IHistogramConverter
{
    public Histogram<byte> ToHistogramModel(RawEventDocument.Histogram histogramDocument);
}