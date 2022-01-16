using OpenCvSharp;

namespace DftMosaic.Core.Mosaic
{
    public class Unmosaicer : IDisposable
    {
        public Rect MosaicedArea { get; }

        public MosaicType MosaicType { get; }

        public Mat? OriginalImage { get; private set; }

        public Mat MosaicedImage { get; }

        public MosaicScale Scale { get; }

        public Unmosaicer(Mat mosaicedImage,
                          Rect mosaicedArea,
                          MosaicType mosaicType,
                          MosaicScale scale)
        {
            this.MosaicedImage = mosaicedImage;
            this.MosaicedArea = mosaicedArea;
            this.MosaicType = mosaicType;
            this.Scale = scale;
        }

        public void Unmosaic()
        {
            var service = new MosaicService();
            switch (this.MosaicType)
            {
                case MosaicType.GrayScale:
                    {
                        using var mosaiced = this.MosaicedImage[this.MosaicedArea].Clone();
                        using var mosaiced32F =
                            new Mat(
                                mosaiced.Rows,
                                mosaiced.Cols, 
                                MatType.CV_32F,
                                mosaiced.Data);
                        using var unmosaiced = service.Unmosaic(mosaiced32F);
                        using var unmosaicedCvt = new Mat();
                        unmosaiced.ConvertTo(unmosaicedCvt, this.MosaicedImage.Type());
                        this.OriginalImage = this.MosaicedImage.CvtColor(ColorConversionCodes.RGBA2GRAY);
                        this.OriginalImage[this.MosaicedArea] = unmosaicedCvt;
                        break;
                    }
                case MosaicType.FullColor:
                    {
                        using var saveMosaiced = this.MosaicedImage[this.MosaicedArea];
                        using var scaledMosaiced = (saveMosaiced - Scalar.All(this.Scale.Beta)) / this.Scale.Alpha;
                        using var unmosaiced = service.Unmosaic(scaledMosaiced);
                        using var original32F = this.MosaicedImage.Clone();
                        original32F[this.MosaicedArea] = unmosaiced;
                        this.OriginalImage = new Mat();
                        original32F.ConvertTo(this.OriginalImage, MatType.CV_8U, 255);
                        break;
                    }
                case MosaicType.Color:
                    {
                        using var saveMosaiced = this.MosaicedImage[this.MosaicedArea];
                        using var saveMosaiced32F = new Mat();
                        saveMosaiced.ConvertTo(saveMosaiced32F, MatType.CV_32F, 1.0 / 65535);
                        using var scaledMosaiced = (saveMosaiced32F - Scalar.All(this.Scale.Beta)) / this.Scale.Alpha;
                        using var unmosaiced = service.Unmosaic(scaledMosaiced);
                        using var unmosaiced8U = new Mat();
                        unmosaiced.ConvertTo(unmosaiced8U, MatType.CV_8U, 255);
                        this.OriginalImage = new Mat();
                        this.MosaicedImage.ConvertTo(this.OriginalImage, MatType.CV_8U, 1.0 / 255);
                        this.OriginalImage[this.MosaicedArea] = unmosaiced8U;
                        break;
                    }
                case MosaicType.ShortColor:
                    {
                        using var saveMosaiced = this.MosaicedImage[this.MosaicedArea];
                        using var saveMosaiced32F = new Mat();
                        saveMosaiced.ConvertTo(saveMosaiced32F, MatType.CV_32F, 1.0 / 255);
                        using var scaledMosaiced = (saveMosaiced32F - Scalar.All(this.Scale.Beta)) / this.Scale.Alpha;
                        using var unmosaiced = service.Unmosaic(scaledMosaiced);
                        using var unmosaiced8U = new Mat();
                        unmosaiced.ConvertTo(unmosaiced8U, MatType.CV_8U, 255);
                        this.OriginalImage = this.MosaicedImage.Clone();
                        this.OriginalImage[this.MosaicedArea] = unmosaiced8U;
                        break;
                    }
            }
        }

        public void Dispose()
        {
        }
    }
}
