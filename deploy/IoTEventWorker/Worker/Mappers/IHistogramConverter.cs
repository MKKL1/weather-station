using Worker.Infrastructure.Documents;
using Worker.Models;

namespace Worker.Mappers;

//TODO move somewhere else, that's not a business service
public interface IHistogramConverter
{
    public Histogram<byte> ToHistogramModel(RawEventDocument.Histogram histogramDocument);
}