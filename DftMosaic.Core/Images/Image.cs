using OpenCvSharp;

namespace DftMosaic.Core.Images
{
    public class Image
    {
        public Mat Data { get; }
        public MosaicInfo? MosaicInfo { get; }

        public bool IsMosaiced => this.MosaicInfo is not null;

        public Image(Mat data, MosaicInfo? mosaicInfo)
        {
            (this.Data, this.MosaicInfo) = (data, mosaicInfo);
        }

        public Image Mosaic(Rect mosaicRequestArea, MosaicType mosaicType, bool optimizeSize = true)
        {
            var mosaicArea = mosaicRequestArea;
            if (optimizeSize)
            {
                mosaicArea = this.DftArea(this.Data, mosaicRequestArea, optimizeSize);
            }

            Mat? mosaicedImage = null;
            MosaicScale? scale = null;
            switch (mosaicType)
            {
                case MosaicType.GrayScale:
                    {
                        using var grayedOriginal = this.Data.CvtColor(ColorConversionCodes.BGRA2GRAY);
                        using var mosaiced = this.MosaicImageData(grayedOriginal[mosaicArea]);
                        mosaicedImage = grayedOriginal.CvtColor(ColorConversionCodes.GRAY2BGRA);
                        using var saveMosaiced = new Mat(mosaiced.Rows, mosaiced.Cols, mosaicedImage.Type(), mosaiced.Data);
                        mosaicedImage[mosaicArea] = saveMosaiced;
                        break;
                    }
                case MosaicType.FullColor:
                    {
                        using var original32F = new Mat();
                        var alpha = this.Data.Depth() switch
                        {
                            0 or 1 => 1.0 / 255,
                            2 or 3 => 1.0 / 65535,
                            _ => 1.0,
                        };
                        this.Data.ConvertTo(original32F, MatType.CV_32F, alpha);
                        using var mosaiced = this.MosaicImageData(original32F[mosaicArea]);
                        scale = this.ScaleImage(mosaiced);
                        mosaicedImage = original32F.Clone();
                        mosaicedImage[mosaicArea] = mosaiced * scale.Alpha + Scalar.All(scale.Beta);
                        break;
                    }
                case MosaicType.Color:
                    {
                        using var original32F = new Mat();
                        var alpha32F = this.Data.Depth() switch
                        {
                            0 or 1 => 1.0 / 255,
                            2 or 3 => 1.0 / 65535,
                            _ => 1.0,
                        };
                        this.Data.ConvertTo(original32F, MatType.CV_32F, alpha32F);
                        using var mosaiced = this.MosaicImageData(original32F[mosaicArea]);
                        mosaicedImage = new Mat();
                        original32F.ConvertTo(mosaicedImage, MatType.CV_16U, 65535);
                        scale = this.ScaleImage(mosaiced);
                        using var scaledMosaiced = (mosaiced * scale.Alpha + Scalar.All(scale.Beta)).ToMat();
                        using var mosaiced16U = new Mat();
                        scaledMosaiced.ConvertTo(mosaiced16U, MatType.CV_16U, 65535);
                        mosaicedImage[mosaicArea] = mosaiced16U;
                        break;
                    }
                case MosaicType.ShortColor:
                    {
                        using var original32F = new Mat();
                        var alpha32F = this.Data.Depth() switch
                        {
                            0 or 1 => 1.0 / 255,
                            2 or 3 => 1.0 / 65535,
                            _ => 1.0,
                        };
                        this.Data.ConvertTo(original32F, MatType.CV_32F, alpha32F);
                        using var mosaiced = this.MosaicImageData(original32F[mosaicArea]);
                        mosaicedImage = new Mat();
                        original32F.ConvertTo(mosaicedImage, MatType.CV_8U, 255);
                        scale = this.ScaleImage(mosaiced);
                        using var scaledMosaiced = (mosaiced * scale.Alpha + Scalar.All(scale.Beta)).ToMat();
                        using var mosaiced8U = new Mat();
                        scaledMosaiced.ConvertTo(mosaiced8U, MatType.CV_8U, 255);
                        mosaicedImage[mosaicArea] = mosaiced8U;
                        break;
                    }
                default:
                    throw new ArgumentException("invalid mosaic type.");
            }
            return new(mosaicedImage, new(mosaicArea, mosaicType, scale));
        }

        public Image Unmosaic()
        {
            if (this.MosaicInfo is null)
            {
                throw new ArgumentException("image is not mosaiced.");
            }

            Mat? unmosaicedImage = null;
            switch (this.MosaicInfo.Type)
            {
                case MosaicType.GrayScale:
                    {
                        using var mosaiced = this.Data[this.MosaicInfo.Area].Clone();
                        using var mosaiced32F =
                            new Mat(
                                mosaiced.Rows,
                                mosaiced.Cols,
                                MatType.CV_32F,
                                mosaiced.Data);
                        using var unmosaiced = this.UnmosaicImageData(mosaiced32F);
                        using var unmosaicedCvt = new Mat();
                        unmosaiced.ConvertTo(unmosaicedCvt, this.Data.Type());
                        unmosaicedImage = this.Data.CvtColor(ColorConversionCodes.RGBA2GRAY);
                        unmosaicedImage[this.MosaicInfo.Area] = unmosaicedCvt;
                        break;
                    }
                case MosaicType.FullColor:
                    {
                        using var saveMosaiced = this.Data[this.MosaicInfo.Area];
                        using var scaledMosaiced = (saveMosaiced - Scalar.All(this.MosaicInfo.Scale.Beta)) / this.MosaicInfo.Scale.Alpha;
                        using var unmosaiced = this.UnmosaicImageData(scaledMosaiced);
                        using var original32F = this.Data.Clone();
                        original32F[this.MosaicInfo.Area] = unmosaiced;
                        unmosaicedImage = new Mat();
                        original32F.ConvertTo(unmosaicedImage, MatType.CV_8U, 255);
                        break;
                    }
                case MosaicType.Color:
                    {
                        using var saveMosaiced = this.Data[this.MosaicInfo.Area];
                        using var saveMosaiced32F = new Mat();
                        saveMosaiced.ConvertTo(saveMosaiced32F, MatType.CV_32F, 1.0 / 65535);
                        using var scaledMosaiced = (saveMosaiced32F - Scalar.All(this.MosaicInfo.Scale.Beta)) / this.MosaicInfo.Scale.Alpha;
                        using var unmosaiced = this.UnmosaicImageData(scaledMosaiced);
                        using var unmosaiced8U = new Mat();
                        unmosaiced.ConvertTo(unmosaiced8U, MatType.CV_8U, 255);
                        unmosaicedImage = new Mat();
                        this.Data.ConvertTo(unmosaicedImage, MatType.CV_8U, 1.0 / 255);
                        unmosaicedImage[this.MosaicInfo.Area] = unmosaiced8U;
                        break;
                    }
                case MosaicType.ShortColor:
                    {
                        using var saveMosaiced = this.Data[this.MosaicInfo.Area];
                        using var saveMosaiced32F = new Mat();
                        saveMosaiced.ConvertTo(saveMosaiced32F, MatType.CV_32F, 1.0 / 255);
                        using var scaledMosaiced = (saveMosaiced32F - Scalar.All(this.MosaicInfo.Scale.Beta)) / this.MosaicInfo.Scale.Alpha;
                        using var unmosaiced = this.UnmosaicImageData(scaledMosaiced);
                        using var unmosaiced8U = new Mat();
                        unmosaiced.ConvertTo(unmosaiced8U, MatType.CV_8U, 255);
                        unmosaicedImage = this.Data.Clone();
                        unmosaicedImage[this.MosaicInfo.Area] = unmosaiced8U;
                        break;
                    }
                default:
                    throw new ArgumentException("invalid mosaic type.");
            }

            return new(unmosaicedImage, null);
        }

        private Mat MosaicImageData(Mat src)
        {
            using var srcF32 = new Mat();
            src.ConvertTo(srcF32, MatType.CV_32F);
            var channels = srcF32.Split();
            var mosaiceds = new Mat[src.Channels()];
            for (int i = 0; i < channels.Length; i++)
            {
                mosaiceds[i] = this.DftOneChannel(channels[i]);
            }

            var mosaiced = new Mat();
            Cv2.Merge(mosaiceds, mosaiced);
            return mosaiced;
        }

        public Mat UnmosaicImageData(Mat src)
        {
            using var srcF32 = new Mat();
            src.ConvertTo(srcF32, MatType.CV_32F);
            var channels = srcF32.Split();
            var unmosaiceds = new Mat[src.Channels()];
            for (int i = 0; i < channels.Length; i++)
            {
                unmosaiceds[i] = this.IdftOneChannel(
                    channels[i]);
            }

            var unmosaiced = new Mat();
            Cv2.Merge(unmosaiceds, unmosaiced);
            return unmosaiced;
        }

        public Rect DftArea(Mat src, Rect requestRect, bool optimizeSize)
        {
            int width = requestRect.Width;
            int height = requestRect.Height;
            if (optimizeSize)
            {
                width = Cv2.GetOptimalDFTSize(width);
                height = Cv2.GetOptimalDFTSize(height);
            }
            width = Math.Min(width, src.Width - requestRect.X);
            height = Math.Min(height, src.Height - requestRect.Y);
            return new(requestRect.X, requestRect.Y, width, height);
        }

        private Mat DftOneChannel(Mat src)
        {
            if (src.Channels() != 1)
            {
                throw new ArgumentException("Channel of the source matrix must be 1.");
            }

            Mat dft = new Mat();
            Cv2.Dft(src, dft, DftFlags.RealOutput);

            return dft;
        }

        private Mat IdftOneChannel(Mat src)
        {
            if (src.Channels() != 1)
            {
                throw new ArgumentException("Channel of the source matrix must be 1.");
            }

            Mat idft = new Mat();
            Cv2.Idft(src, idft, DftFlags.RealOutput | DftFlags.Scale);

            return idft;
        }

        public MosaicScale Scale(Mat mat)
        {
            mat.MinMaxIdx(out double min, out double max);
            var alpha = 1.0 / (max - min);
            var beta = -min * alpha;
            return new MosaicScale
            {
                Alpha = alpha,
                Beta = beta,
            };
        }

        private MosaicScale ScaleImage(Mat mat)
        {
            mat.MinMaxIdx(out var matMin, out var matMax);
            var alpha = 1.0 / (matMax - matMin);
            var beta = -matMin * alpha;
            return new MosaicScale { Alpha = alpha, Beta = beta };
        }
    }
}