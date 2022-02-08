using OpenCvSharp;

namespace DftMosaic.Core.Images
{
    public record MosaicRequestArea(Rect Area, double Angle)
    {
    }
}
