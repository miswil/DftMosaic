using OpenCvSharp;

namespace DftMosaic.Core.Images
{
    public record MosaicInfo(Rect Area, MosaicType Type, MosaicScale? Scale)
    {
    }
}
