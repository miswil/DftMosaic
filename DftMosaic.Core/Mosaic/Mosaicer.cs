using OpenCvSharp;

namespace DftMosaic.Core.Mosaic
{
    public sealed record Mosaicer : IDisposable
    {
        public Rect MosaicedArea { get; private set; }

        public MosaicType MosaicType { get; private set; }

        public Mat OriginalImage { get; private set; }

        public Mat? MosaicedImage { get; private set; }

        public MosaicScale Scale { get; private set; }

        public Mosaicer(Mat originalImage)
        {
            this.OriginalImage = originalImage;
        }

        public void Mosaic(Rect mosaicRequestArea, MosaicType mosaicType, bool optimizeSize = true)
        {
            var service = new MosaicService();
            this.MosaicType = mosaicType;
            this.MosaicedArea = service.DftArea(this.OriginalImage, mosaicRequestArea, optimizeSize);
            switch (mosaicType)
            {
                case MosaicType.GrayScale:
                    {
                        using var original = this.OriginalImage.CvtColor(ColorConversionCodes.BGRA2GRAY);
                        using var mosaiced = service.Mosaic(original[this.MosaicedArea]);
                        this.MosaicedImage = original.CvtColor(ColorConversionCodes.GRAY2BGRA);
                        using var saveMosaiced = new Mat(mosaiced.Rows, mosaiced.Cols, this.MosaicedImage.Type(), mosaiced.Data);
                        this.MosaicedImage[this.MosaicedArea] = saveMosaiced;
                        break;
                    }
                case MosaicType.FullColor:
                    {
                        using var original32F = new Mat();
                        var alpha = this.OriginalImage.Depth() switch
                            {
                                0 or 1 => 1.0 / 255,
                                2 or 3 => 1.0 / 65535,
                                _ => 1.0,
                            };
                        this.OriginalImage.ConvertTo(original32F, MatType.CV_32F, alpha);
                        using var mosaiced = service.Mosaic(original32F[this.MosaicedArea]);
                        var scale = this.ScaleImage(mosaiced);
                        this.MosaicedImage = original32F.Clone();
                        this.MosaicedImage[this.MosaicedArea] = mosaiced * scale.Alpha + Scalar.All(scale.Beta);
                        this.Scale = scale;
                        break;
                    }
                case MosaicType.Color:
                    {
                        using var original32F = new Mat();
                        var alpha32F = this.OriginalImage.Depth() switch
                        {
                            0 or 1 => 1.0 / 255,
                            2 or 3 => 1.0 / 65535,
                            _ => 1.0,
                        };
                        this.OriginalImage.ConvertTo(original32F, MatType.CV_32F, alpha32F);
                        using var mosaiced = service.Mosaic(original32F[this.MosaicedArea]);
                        this.MosaicedImage = new Mat();
                        original32F.ConvertTo(this.MosaicedImage, MatType.CV_16U, 65535);
                        var scale = this.ScaleImage(mosaiced);
                        using var scaledMosaiced = (mosaiced * scale.Alpha + Scalar.All(scale.Beta)).ToMat();
                        using var mosaiced16U = new Mat();
                        scaledMosaiced.ConvertTo(mosaiced16U, MatType.CV_16U, 65535);
                        this.MosaicedImage[this.MosaicedArea] = mosaiced16U;
                        this.Scale = scale;
                        break;
                    }
                case MosaicType.ShortColor:
                    {
                        using var original32F = new Mat();
                        var alpha32F = this.OriginalImage.Depth() switch
                        {
                            0 or 1 => 1.0 / 255,
                            2 or 3 => 1.0 / 65535,
                            _ => 1.0,
                        };
                        this.OriginalImage.ConvertTo(original32F, MatType.CV_32F, alpha32F);
                        using var mosaiced = service.Mosaic(original32F[this.MosaicedArea]);
                        this.MosaicedImage = new Mat();
                        original32F.ConvertTo(this.MosaicedImage, MatType.CV_8U, 255);
                        var scale = this.ScaleImage(mosaiced);
                        using var scaledMosaiced = (mosaiced * scale.Alpha + Scalar.All(scale.Beta)).ToMat();
                        using var mosaiced8U = new Mat();
                        scaledMosaiced.ConvertTo(mosaiced8U, MatType.CV_8U, 255);
                        this.MosaicedImage[this.MosaicedArea] = mosaiced8U;
                        this.Scale = scale;
                        break;
                    }
            }
        }

        private MosaicScale ScaleImage(Mat mat)
        {
            mat.MinMaxIdx(out var matMin, out var matMax);
            var alpha = 1.0 / (matMax - matMin);
            var beta = - matMin * alpha;
            return new MosaicScale { Alpha = alpha, Beta = beta };
        }

        public void Dispose()
        {
        }
    }
}
