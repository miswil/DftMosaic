using OpenCvSharp;

namespace DftMosaic.Core.Images
{
    public record MosaicArea(Rect Area, MosaicScale? Scale)
    {
    }
}
