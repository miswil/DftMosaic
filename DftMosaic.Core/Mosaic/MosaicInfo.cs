using OpenCvSharp;

namespace DftMosaic.Core.Mosaic
{
    public partial class MosaicInfo
    {
        public Rect Area { get; set; }

        public MosaicType Type { get; set; }

        public MosaicScale Scale { get; set; }
    }
}
